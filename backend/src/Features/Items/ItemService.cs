using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Items;

public sealed class ItemService(AppDbContext dbContext, TimeProvider timeProvider) : IItemService
{
  private const int DefaultLimit = 20;
  private const int MaxLimit = 50;
  private const int MaxNotesLength = 2000;

  private readonly AppDbContext _dbContext = dbContext;
  private readonly TimeProvider _timeProvider = timeProvider;

  public async Task<ItemServiceResult<ItemDto>> CreateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CreateItemRequestDto request,
    CancellationToken cancellationToken)
  {
    var wishlistResult = await ResolveOwnerWishlistAsync(ownerUserId, wishlistId, cancellationToken);
    if (!wishlistResult.IsSuccess)
    {
      return ItemServiceResult<ItemDto>.Failure(wishlistResult.ErrorCode!);
    }

    var normalized = NormalizeAndValidate(request.Name, request.Url, request.PriceAmount, request.PriceCurrency, request.Priority, request.Notes);
    if (!normalized.IsSuccess)
    {
      return ItemServiceResult<ItemDto>.Failure(ItemErrorCodes.ValidationFailed);
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;

    var item = new WishItem
    {
      WishlistId = wishlistId,
      Name = normalized.Name!,
      Url = normalized.Url,
      PriceAmount = normalized.PriceAmount,
      PriceCurrency = normalized.PriceCurrency,
      Priority = normalized.Priority,
      Notes = normalized.Notes,
      CreatedAtUtc = now,
      UpdatedAtUtc = now,
      IsDeleted = false
    };

    _dbContext.WishItems.Add(item);
    await _dbContext.SaveChangesAsync(cancellationToken);

    return ItemServiceResult<ItemDto>.Success(ToDto(item));
  }

  public async Task<ItemServiceResult<ItemListResult>> ListAsync(
    Guid ownerUserId,
    Guid wishlistId,
    ItemListQuery query,
    CancellationToken cancellationToken)
  {
    var wishlistResult = await ResolveOwnerWishlistAsync(ownerUserId, wishlistId, cancellationToken);
    if (!wishlistResult.IsSuccess)
    {
      return ItemServiceResult<ItemListResult>.Failure(wishlistResult.ErrorCode!);
    }

    var limit = NormalizeLimit(query.Limit);

    var baseQuery = _dbContext.WishItems
      .AsNoTracking()
      .Where(item => item.WishlistId == wishlistId && !item.IsDeleted);

    var hasCursor = TryParseCursor(query.Cursor, out var cursorUpdatedAt, out var cursorItemId);
    if (hasCursor)
    {
      baseQuery = baseQuery.Where(item => item.UpdatedAtUtc <= cursorUpdatedAt);
    }

    var candidates = await baseQuery
      .OrderByDescending(item => item.UpdatedAtUtc)
      .ThenByDescending(item => item.Id)
      .Take(limit + 256)
      .Select(item => new ItemProjection(
        item.Id,
        item.WishlistId,
        item.Name,
        item.Url,
        item.PriceAmount,
        item.PriceCurrency,
        item.Priority,
        item.Notes,
        item.UpdatedAtUtc))
      .ToListAsync(cancellationToken);

    if (hasCursor)
    {
      candidates = candidates
        .Where(item =>
          item.UpdatedAtUtc < cursorUpdatedAt
          || (item.UpdatedAtUtc == cursorUpdatedAt && item.Id < cursorItemId))
        .ToList();
    }

    var page = candidates.Take(limit + 1).ToList();
    var hasNext = page.Count > limit;
    if (hasNext)
    {
      page.RemoveAt(page.Count - 1);
    }

    var items = page
      .Select(item => new ItemDto(
        item.Id,
        item.WishlistId,
        item.Name,
        item.Url,
        item.PriceAmount,
        item.PriceCurrency,
        item.Priority,
        item.Notes,
        item.UpdatedAtUtc))
      .ToList();

    var nextCursor = hasNext && items.Count > 0
      ? EncodeCursor(items[^1].UpdatedAtUtc, items[^1].Id)
      : null;

    return ItemServiceResult<ItemListResult>.Success(new ItemListResult(items, nextCursor));
  }

  public async Task<ItemServiceResult<ItemDto>> UpdateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    int itemId,
    UpdateItemRequestDto request,
    CancellationToken cancellationToken)
  {
    var wishlistResult = await ResolveOwnerWishlistAsync(ownerUserId, wishlistId, cancellationToken);
    if (!wishlistResult.IsSuccess)
    {
      return ItemServiceResult<ItemDto>.Failure(wishlistResult.ErrorCode!);
    }

    if (request is { Name: null, Url: null, PriceAmount: null, PriceCurrency: null, Priority: null, Notes: null })
    {
      return ItemServiceResult<ItemDto>.Failure(ItemErrorCodes.ValidationFailed);
    }

    var item = await _dbContext.WishItems
      .FirstOrDefaultAsync(x => x.WishlistId == wishlistId && x.Id == itemId && !x.IsDeleted, cancellationToken);

    if (item is null)
    {
      return ItemServiceResult<ItemDto>.Failure(ItemErrorCodes.NotFound);
    }

    var mergedName = request.Name ?? item.Name;
    var mergedUrl = request.Url ?? item.Url;
    var mergedPriceAmount = request.PriceAmount ?? item.PriceAmount;
    var mergedPriceCurrency = request.PriceCurrency ?? item.PriceCurrency;
    var mergedPriority = request.Priority ?? item.Priority;
    var mergedNotes = request.Notes ?? item.Notes;

    var normalized = NormalizeAndValidate(
      mergedName,
      mergedUrl,
      mergedPriceAmount,
      mergedPriceCurrency,
      mergedPriority,
      mergedNotes);

    if (!normalized.IsSuccess)
    {
      return ItemServiceResult<ItemDto>.Failure(ItemErrorCodes.ValidationFailed);
    }

    item.Name = normalized.Name!;
    item.Url = normalized.Url;
    item.PriceAmount = normalized.PriceAmount;
    item.PriceCurrency = normalized.PriceCurrency;
    item.Priority = normalized.Priority;
    item.Notes = normalized.Notes;
    item.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

    await _dbContext.SaveChangesAsync(cancellationToken);

    return ItemServiceResult<ItemDto>.Success(ToDto(item));
  }

  public async Task<ItemServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid wishlistId,
    int itemId,
    CancellationToken cancellationToken)
  {
    var wishlistResult = await ResolveOwnerWishlistAsync(ownerUserId, wishlistId, cancellationToken);
    if (!wishlistResult.IsSuccess)
    {
      return ItemServiceResult<bool>.Failure(wishlistResult.ErrorCode!);
    }

    var item = await _dbContext.WishItems
      .FirstOrDefaultAsync(x => x.WishlistId == wishlistId && x.Id == itemId && !x.IsDeleted, cancellationToken);

    if (item is null)
    {
      return ItemServiceResult<bool>.Failure(ItemErrorCodes.NotFound);
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;
    item.IsDeleted = true;
    item.DeletedAtUtc = now;
    item.UpdatedAtUtc = now;

    await _dbContext.SaveChangesAsync(cancellationToken);

    return ItemServiceResult<bool>.Success(true);
  }

  private async Task<ItemServiceResult<bool>> ResolveOwnerWishlistAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == wishlistId && !x.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return ItemServiceResult<bool>.Failure(ItemErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return ItemServiceResult<bool>.Failure(ItemErrorCodes.Forbidden);
    }

    return ItemServiceResult<bool>.Success(true);
  }

  private static ItemValidationResult NormalizeAndValidate(
    string? name,
    string? url,
    decimal? priceAmount,
    string? priceCurrency,
    int priority,
    string? notes)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      return ItemValidationResult.Failure();
    }

    var normalizedName = name.Trim();
    if (normalizedName.Length > 240)
    {
      return ItemValidationResult.Failure();
    }

    var normalizedUrl = NormalizeUrl(url);
    if (url is not null && normalizedUrl is null)
    {
      return ItemValidationResult.Failure();
    }

    var normalizedCurrency = NormalizeCurrency(priceCurrency);

    if (normalizedCurrency is not null && !priceAmount.HasValue)
    {
      return ItemValidationResult.Failure();
    }

    if (priceAmount.HasValue && normalizedCurrency is null)
    {
      return ItemValidationResult.Failure();
    }

    if (priceAmount.HasValue && priceAmount.Value < 0)
    {
      return ItemValidationResult.Failure();
    }

    if (priority is < 0 or > 5)
    {
      return ItemValidationResult.Failure();
    }

    var normalizedNotes = notes?.Trim();
    if (normalizedNotes is { Length: > MaxNotesLength })
    {
      return ItemValidationResult.Failure();
    }

    if (string.IsNullOrWhiteSpace(normalizedNotes))
    {
      normalizedNotes = null;
    }

    return ItemValidationResult.Success(normalizedName, normalizedUrl, priceAmount, normalizedCurrency, priority, normalizedNotes);
  }

  private static string? NormalizeUrl(string? rawUrl)
  {
    if (rawUrl is null)
    {
      return null;
    }

    var trimmed = rawUrl.Trim();
    if (trimmed.Length == 0)
    {
      return null;
    }

    if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
    {
      if (absoluteUri.Scheme is "http" or "https")
      {
        return absoluteUri.ToString();
      }

      return null;
    }

    var withScheme = $"https://{trimmed}";
    if (Uri.TryCreate(withScheme, UriKind.Absolute, out var withHttps)
      && withHttps.Scheme is "http" or "https")
    {
      return withHttps.ToString();
    }

    return null;
  }

  private static string? NormalizeCurrency(string? priceCurrency)
  {
    if (priceCurrency is null)
    {
      return null;
    }

    var trimmed = priceCurrency.Trim();
    if (trimmed.Length == 0)
    {
      return null;
    }

    if (trimmed.Length != 3 || !trimmed.All(char.IsLetter))
    {
      return null;
    }

    return trimmed.ToUpperInvariant();
  }

  private static int NormalizeLimit(int? limit)
  {
    if (limit is null || limit <= 0)
    {
      return DefaultLimit;
    }

    return Math.Min(limit.Value, MaxLimit);
  }

  private static string EncodeCursor(DateTime updatedAtUtc, int itemId)
  {
    var raw = $"{updatedAtUtc.Ticks}:{itemId}";
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParseCursor(string? cursor, out DateTime updatedAtUtc, out int itemId)
  {
    updatedAtUtc = default;
    itemId = default;

    if (string.IsNullOrWhiteSpace(cursor))
    {
      return false;
    }

    try
    {
      var bytes = WebEncoders.Base64UrlDecode(cursor);
      var raw = Encoding.UTF8.GetString(bytes);
      var parts = raw.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length != 2)
      {
        return false;
      }

      if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
      {
        return false;
      }

      if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out itemId))
      {
        return false;
      }

      updatedAtUtc = new DateTime(ticks, DateTimeKind.Utc);
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static ItemDto ToDto(WishItem item)
  {
    return new ItemDto(
      item.Id,
      item.WishlistId,
      item.Name,
      item.Url,
      item.PriceAmount,
      item.PriceCurrency,
      item.Priority,
      item.Notes,
      item.UpdatedAtUtc);
  }

  private sealed record ItemProjection(
    int Id,
    Guid WishlistId,
    string Name,
    string? Url,
    decimal? PriceAmount,
    string? PriceCurrency,
    int Priority,
    string? Notes,
    DateTime UpdatedAtUtc);

  private sealed record ItemValidationResult(
    bool IsSuccess,
    string? Name,
    string? Url,
    decimal? PriceAmount,
    string? PriceCurrency,
    int Priority,
    string? Notes)
  {
    public static ItemValidationResult Success(
      string name,
      string? url,
      decimal? priceAmount,
      string? priceCurrency,
      int priority,
      string? notes) =>
      new(true, name, url, priceAmount, priceCurrency, priority, notes);

    public static ItemValidationResult Failure() => new(false, null, null, null, null, 0, null);
  }
}
