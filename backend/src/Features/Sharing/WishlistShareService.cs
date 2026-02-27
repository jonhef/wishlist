using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Features.Fx;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Sharing;

public sealed class WishlistShareService(
  AppDbContext dbContext,
  IFxRatesService fxRatesService) : IWishlistShareService
{
  private const int DefaultLimit = 20;
  private const int MaxLimit = 50;

  private readonly AppDbContext _dbContext = dbContext;
  private readonly IFxRatesService _fxRatesService = fxRatesService;

  public async Task<WishlistShareServiceResult<ShareRotationResult>> RotateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(x => x.Id == wishlistId && !x.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<ShareRotationResult>.Failure(WishlistShareErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistShareServiceResult<ShareRotationResult>.Failure(WishlistShareErrorCodes.Forbidden);
    }

    var token = GenerateToken();
    wishlist.ShareTokenHash = ComputeTokenHash(token);

    await _dbContext.SaveChangesAsync(cancellationToken);

    return WishlistShareServiceResult<ShareRotationResult>.Success(new ShareRotationResult(token));
  }

  public async Task<WishlistShareServiceResult<bool>> DisableAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(x => x.Id == wishlistId && !x.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<bool>.Failure(WishlistShareErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistShareServiceResult<bool>.Failure(WishlistShareErrorCodes.Forbidden);
    }

    wishlist.ShareTokenHash = null;
    await _dbContext.SaveChangesAsync(cancellationToken);

    return WishlistShareServiceResult<bool>.Success(true);
  }

  public async Task<WishlistShareServiceResult<PublicWishlistDto>> GetPublicByTokenAsync(
    string token,
    PublicWishlistListQuery query,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.NotFound);
    }

    var tokenHash = ComputeTokenHash(token);

    var wishlist = await _dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(
        x => x.ShareTokenHash == tokenHash && !x.IsDeleted,
        cancellationToken);

    if (wishlist is null)
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.NotFound);
    }

    if (query.Sort == PublicWishlistSort.price)
    {
      return await GetPriceSortedPublicAsync(wishlist, query, cancellationToken);
    }

    var limit = NormalizeLimit(query.Limit);
    var itemsQuery = _dbContext.WishItems
      .AsNoTracking()
      .Where(x => x.WishlistId == wishlist.Id && !x.IsDeleted);

    if (query.Sort == PublicWishlistSort.priority)
    {
      var hasPriorityCursor = TryParsePriorityCursor(query.Cursor, out var cursorPriority, out var cursorCreatedAt, out var cursorId);
      if (hasPriorityCursor)
      {
        itemsQuery = itemsQuery.Where(item =>
          item.Priority < cursorPriority
          || (item.Priority == cursorPriority && item.CreatedAtUtc < cursorCreatedAt)
          || (item.Priority == cursorPriority && item.CreatedAtUtc == cursorCreatedAt && item.Id < cursorId));
      }

      itemsQuery = itemsQuery
        .OrderByDescending(x => x.Priority)
        .ThenByDescending(x => x.CreatedAtUtc)
        .ThenByDescending(x => x.Id);
    }
    else
    {
      var hasAddedCursor = TryParseAddedCursor(query.Cursor, out var cursorCreatedAt, out var cursorId);
      if (hasAddedCursor)
      {
        itemsQuery = itemsQuery.Where(item =>
          item.CreatedAtUtc < cursorCreatedAt
          || (item.CreatedAtUtc == cursorCreatedAt && item.Id < cursorId));
      }

      itemsQuery = itemsQuery
        .OrderByDescending(x => x.CreatedAtUtc)
        .ThenByDescending(x => x.Id);
    }

    var candidates = await itemsQuery
      .Take(limit + 1)
      .Select(x => new PublicWishlistItemProjection(
        x.Name,
        x.Url,
        x.PriceAmount,
        x.PriceCurrency,
        x.Priority,
        x.Notes,
        x.CreatedAtUtc,
        x.Id))
      .ToListAsync(cancellationToken);

    var hasNext = candidates.Count > limit;
    if (hasNext)
    {
      candidates.RemoveAt(candidates.Count - 1);
    }

    var page = candidates
      .Select(x => new PublicWishlistItemDto(
        x.Id,
        x.Name,
        x.Url,
        x.PriceAmount,
        x.PriceCurrency,
        x.Notes,
        x.CreatedAtUtc))
      .ToList();

    var nextCursor = hasNext && candidates.Count > 0
      ? query.Sort == PublicWishlistSort.priority
        ? EncodePriorityCursor(candidates[^1].Priority, candidates[^1].CreatedAtUtc, candidates[^1].Id)
        : EncodeAddedCursor(candidates[^1].CreatedAtUtc, candidates[^1].Id)
      : null;

    var themeTokens = await ResolveThemeTokensAsync(wishlist.ThemeId, wishlist.OwnerUserId, cancellationToken);

    var payload = new PublicWishlistDto(
      wishlist.Title,
      wishlist.Description,
      themeTokens,
      page,
      nextCursor);

    return WishlistShareServiceResult<PublicWishlistDto>.Success(payload);
  }

  private async Task<WishlistShareServiceResult<PublicWishlistDto>> GetPriceSortedPublicAsync(
    Domain.Entities.WishlistEntity wishlist,
    PublicWishlistListQuery query,
    CancellationToken cancellationToken)
  {
    var snapshot = await _fxRatesService.GetLatestSnapshotAsync(cancellationToken);
    if (snapshot is null)
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.FxUnavailable);
    }

    var wishlistBaseCurrency = SupportedCurrencies.IsSupported(wishlist.BaseCurrency)
      ? wishlist.BaseCurrency
      : SupportedCurrencies.Eur;

    if (!snapshot.RatesByQuote.ContainsKey(wishlistBaseCurrency))
    {
      return WishlistShareServiceResult<PublicWishlistDto>.Failure(WishlistShareErrorCodes.FxUnavailable);
    }

    var items = await _dbContext.WishItems
      .AsNoTracking()
      .Where(item => item.WishlistId == wishlist.Id && !item.IsDeleted)
      .Select(item => new PublicWishlistItemProjection(
        item.Name,
        item.Url,
        item.PriceAmount,
        item.PriceCurrency,
        item.Priority,
        item.Notes,
        item.CreatedAtUtc,
        item.Id))
      .ToListAsync(cancellationToken);

    var ordered = OrderPriceItems(
      items,
      snapshot,
      wishlistBaseCurrency,
      query.Order);

    var startIndex = 0;
    if (TryParsePriceCursor(query.Cursor, out var cursorItemId))
    {
      var cursorIndex = ordered.FindIndex(item => item.Item.Id == cursorItemId);
      if (cursorIndex >= 0)
      {
        startIndex = cursorIndex + 1;
      }
    }

    var limit = NormalizeLimit(query.Limit);
    var page = ordered.Skip(startIndex).Take(limit).Select(item => item.Item).ToList();
    var hasNext = ordered.Count > startIndex + page.Count;

    var payloadItems = page
      .Select(item => new PublicWishlistItemDto(
        item.Id,
        item.Name,
        item.Url,
        item.PriceAmount,
        item.PriceCurrency,
        item.Notes,
        item.CreatedAtUtc))
      .ToList();

    var nextCursor = hasNext && payloadItems.Count > 0
      ? EncodePriceCursor(payloadItems[^1].Id)
      : null;

    var themeTokens = await ResolveThemeTokensAsync(wishlist.ThemeId, wishlist.OwnerUserId, cancellationToken);
    var payload = new PublicWishlistDto(
      wishlist.Title,
      wishlist.Description,
      themeTokens,
      payloadItems,
      nextCursor);

    return WishlistShareServiceResult<PublicWishlistDto>.Success(payload);
  }

  private async Task<ThemeTokensDto> ResolveThemeTokensAsync(
    Guid? themeId,
    Guid ownerUserId,
    CancellationToken cancellationToken)
  {
    if (!themeId.HasValue)
    {
      return ThemeTokenDefaults.CreateDefault();
    }

    var tokensJson = await _dbContext.Themes
      .AsNoTracking()
      .Where(theme => theme.Id == themeId.Value && theme.OwnerUserId == ownerUserId)
      .Select(theme => theme.TokensJson)
      .FirstOrDefaultAsync(cancellationToken);

    return ThemeTokenDefaults.ParseAndNormalize(tokensJson);
  }

  private static string GenerateToken()
  {
    var bytes = RandomNumberGenerator.GetBytes(32);
    return WebEncoders.Base64UrlEncode(bytes);
  }

  private static string ComputeTokenHash(string token)
  {
    var bytes = Encoding.UTF8.GetBytes(token);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash);
  }

  private static int NormalizeLimit(int? limit)
  {
    if (limit is null || limit <= 0)
    {
      return DefaultLimit;
    }

    return Math.Min(limit.Value, MaxLimit);
  }

  private static string EncodePriorityCursor(decimal priority, DateTime createdAtUtc, int itemId)
  {
    var raw = $"{priority.ToString(CultureInfo.InvariantCulture)}:{createdAtUtc.Ticks}:{itemId}";
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParsePriorityCursor(
    string? cursor,
    out decimal priority,
    out DateTime createdAtUtc,
    out int itemId)
  {
    priority = default;
    createdAtUtc = default;
    itemId = default;

    if (string.IsNullOrWhiteSpace(cursor))
    {
      return false;
    }

    try
    {
      var bytes = WebEncoders.Base64UrlDecode(cursor);
      var raw = Encoding.UTF8.GetString(bytes);
      var parts = raw.Split(':', 3, StringSplitOptions.RemoveEmptyEntries);

      if (parts.Length != 3)
      {
        return false;
      }

      if (!decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out priority))
      {
        return false;
      }

      if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
      {
        return false;
      }

      if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out itemId))
      {
        return false;
      }

      createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static string EncodeAddedCursor(DateTime createdAtUtc, int itemId)
  {
    var raw = $"{createdAtUtc.Ticks}:{itemId}";
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParseAddedCursor(
    string? cursor,
    out DateTime createdAtUtc,
    out int itemId)
  {
    createdAtUtc = default;
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

      createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static string EncodePriceCursor(int itemId)
  {
    var raw = itemId.ToString(CultureInfo.InvariantCulture);
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParsePriceCursor(string? cursor, out int itemId)
  {
    itemId = default;
    if (string.IsNullOrWhiteSpace(cursor))
    {
      return false;
    }

    try
    {
      var bytes = WebEncoders.Base64UrlDecode(cursor);
      var raw = Encoding.UTF8.GetString(bytes);
      return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out itemId);
    }
    catch
    {
      return false;
    }
  }

  private static List<PriceSortedItem> OrderPriceItems(
    IReadOnlyList<PublicWishlistItemProjection> items,
    FxRatesSnapshot snapshot,
    string wishlistBaseCurrency,
    PublicWishlistOrder order)
  {
    var priced = items
      .Select(item => new PriceSortedItem(
        item,
        PriceNormalization.TryNormalizeMinorToBase(item.PriceAmount, item.PriceCurrency, wishlistBaseCurrency, snapshot.RatesByQuote)))
      .ToList();

    var ordered = order == PublicWishlistOrder.desc
      ? priced
        .OrderBy(item => !item.PriceInBase.HasValue ? 1 : 0)
        .ThenByDescending(item => item.PriceInBase)
      : priced
        .OrderBy(item => !item.PriceInBase.HasValue ? 1 : 0)
        .ThenBy(item => item.PriceInBase);

    return ordered
      .ThenByDescending(item => item.Item.CreatedAtUtc)
      .ThenByDescending(item => item.Item.Id)
      .ToList();
  }

  private sealed record PublicWishlistItemProjection(
    string Name,
    string? Url,
    int? PriceAmount,
    string? PriceCurrency,
    decimal Priority,
    string? Notes,
    DateTime CreatedAtUtc,
    int Id);

  private sealed record PriceSortedItem(PublicWishlistItemProjection Item, decimal? PriceInBase);
}
