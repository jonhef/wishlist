using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Wishlists;

public sealed class WishlistService(AppDbContext dbContext, TimeProvider timeProvider) : IWishlistService
{
  private const int DefaultLimit = 20;
  private const int MaxLimit = 50;

  private readonly AppDbContext _dbContext = dbContext;
  private readonly TimeProvider _timeProvider = timeProvider;

  public async Task<WishlistServiceResult<WishlistDto>> CreateAsync(
    Guid ownerUserId,
    CreateWishlistRequestDto request,
    CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(request.Title))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ValidationFailed);
    }

    if (!await OwnerExistsAsync(ownerUserId, cancellationToken))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.Forbidden);
    }

    if (!await ThemeIsAccessibleAsync(ownerUserId, request.ThemeId, cancellationToken))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ThemeNotAccessible);
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;

    var wishlist = new WishlistEntity
    {
      OwnerUserId = ownerUserId,
      Title = request.Title.Trim(),
      Description = NormalizeOptionalString(request.Description),
      ThemeId = request.ThemeId,
      CreatedAtUtc = now,
      UpdatedAtUtc = now,
      IsDeleted = false
    };

    _dbContext.Wishlists.Add(wishlist);

    try
    {
      await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (IsForeignKeyConstraint(ex))
    {
      return WishlistServiceResult<WishlistDto>.Failure(
        request.ThemeId.HasValue
          ? WishlistErrorCodes.ThemeNotAccessible
          : WishlistErrorCodes.Forbidden);
    }

    return WishlistServiceResult<WishlistDto>.Success(ToDto(wishlist, itemsCount: 0));
  }

  public async Task<WishlistServiceResult<WishlistListResult>> ListAsync(
    Guid ownerUserId,
    WishlistListQuery query,
    CancellationToken cancellationToken)
  {
    var limit = NormalizeLimit(query.Limit);

    var baseQuery = _dbContext.Wishlists
      .AsNoTracking()
      .Where(wishlist => wishlist.OwnerUserId == ownerUserId && !wishlist.IsDeleted);

    var hasCursor = TryParseCursor(query.Cursor, out var cursorUpdatedAt, out var cursorId);

    if (hasCursor)
    {
      baseQuery = baseQuery.Where(wishlist => wishlist.UpdatedAtUtc <= cursorUpdatedAt);
    }

    var candidates = await baseQuery
      .OrderByDescending(wishlist => wishlist.UpdatedAtUtc)
      .ThenByDescending(wishlist => wishlist.Id)
      .Take(limit + 256)
      .Select(wishlist => new WishlistProjection(
        wishlist.Id,
        wishlist.Title,
        wishlist.Description,
        wishlist.ThemeId,
        wishlist.UpdatedAtUtc))
      .ToListAsync(cancellationToken);

    if (hasCursor)
    {
      candidates = candidates
        .Where(wishlist =>
          wishlist.UpdatedAtUtc < cursorUpdatedAt
          || (wishlist.UpdatedAtUtc == cursorUpdatedAt && wishlist.Id.CompareTo(cursorId) < 0))
        .ToList();
    }

    var page = candidates.Take(limit + 1).ToList();

    var hasNext = page.Count > limit;
    if (hasNext)
    {
      page.RemoveAt(page.Count - 1);
    }

    var wishlistIds = page.Select(row => row.Id).ToList();
    var itemsCountMap = await _dbContext.WishItems
      .AsNoTracking()
      .Where(item => wishlistIds.Contains(item.WishlistId) && !item.IsDeleted)
      .GroupBy(item => item.WishlistId)
      .Select(group => new { WishlistId = group.Key, Count = group.Count() })
      .ToDictionaryAsync(x => x.WishlistId, x => x.Count, cancellationToken);

    var items = page
      .Select(row => new WishlistDto(
        row.Id,
        row.Title,
        row.Description,
        row.ThemeId,
        row.UpdatedAtUtc,
        itemsCountMap.GetValueOrDefault(row.Id, 0)))
      .ToList();

    var nextCursor = hasNext && items.Count > 0
      ? EncodeCursor(items[^1].UpdatedAt, items[^1].Id)
      : null;

    return WishlistServiceResult<WishlistListResult>.Success(new WishlistListResult(items, nextCursor));
  }

  public async Task<WishlistServiceResult<WishlistDto>> GetByIdAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .AsNoTracking()
      .FirstOrDefaultAsync(item => item.Id == wishlistId && !item.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.Forbidden);
    }

    var itemsCount = await _dbContext.WishItems
      .AsNoTracking()
      .CountAsync(item => item.WishlistId == wishlist.Id && !item.IsDeleted, cancellationToken);

    return WishlistServiceResult<WishlistDto>.Success(ToDto(wishlist, itemsCount));
  }

  public async Task<WishlistServiceResult<WishlistDto>> UpdateAsync(
    Guid ownerUserId,
    Guid wishlistId,
    UpdateWishlistRequestDto request,
    CancellationToken cancellationToken)
  {
    var hasAnyPatchValue = request.Title is not null || request.Description is not null || request.ThemeId.HasValue;

    if (!hasAnyPatchValue)
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ValidationFailed);
    }

    if (request.Title is { } title && string.IsNullOrWhiteSpace(title))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ValidationFailed);
    }

    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(item => item.Id == wishlistId && !item.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.Forbidden);
    }

    if (request.ThemeId.HasValue && !await ThemeIsAccessibleAsync(ownerUserId, request.ThemeId, cancellationToken))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ThemeNotAccessible);
    }

    if (request.Title is not null)
    {
      wishlist.Title = request.Title.Trim();
    }

    if (request.Description is not null)
    {
      wishlist.Description = NormalizeOptionalString(request.Description);
    }

    if (request.ThemeId.HasValue)
    {
      wishlist.ThemeId = request.ThemeId;
    }

    wishlist.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

    try
    {
      await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (IsForeignKeyConstraint(ex))
    {
      return WishlistServiceResult<WishlistDto>.Failure(WishlistErrorCodes.ThemeNotAccessible);
    }

    var itemsCount = await _dbContext.WishItems
      .AsNoTracking()
      .CountAsync(item => item.WishlistId == wishlist.Id && !item.IsDeleted, cancellationToken);

    return WishlistServiceResult<WishlistDto>.Success(ToDto(wishlist, itemsCount));
  }

  public async Task<WishlistServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid wishlistId,
    CancellationToken cancellationToken)
  {
    var wishlist = await _dbContext.Wishlists
      .FirstOrDefaultAsync(item => item.Id == wishlistId && !item.IsDeleted, cancellationToken);

    if (wishlist is null)
    {
      return WishlistServiceResult<bool>.Failure(WishlistErrorCodes.NotFound);
    }

    if (wishlist.OwnerUserId != ownerUserId)
    {
      return WishlistServiceResult<bool>.Failure(WishlistErrorCodes.Forbidden);
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;
    wishlist.IsDeleted = true;
    wishlist.DeletedAtUtc = now;
    wishlist.UpdatedAtUtc = now;

    await _dbContext.SaveChangesAsync(cancellationToken);

    return WishlistServiceResult<bool>.Success(true);
  }

  private async Task<bool> ThemeIsAccessibleAsync(Guid ownerUserId, Guid? themeId, CancellationToken cancellationToken)
  {
    if (!themeId.HasValue)
    {
      return true;
    }

    return await _dbContext.Themes.AnyAsync(
      theme => theme.Id == themeId.Value && theme.OwnerUserId == ownerUserId,
      cancellationToken);
  }

  private async Task<bool> OwnerExistsAsync(Guid ownerUserId, CancellationToken cancellationToken)
  {
    return await _dbContext.Users
      .AsNoTracking()
      .AnyAsync(user => user.Id == ownerUserId, cancellationToken);
  }

  private static string? NormalizeOptionalString(string? value)
  {
    if (value is null)
    {
      return null;
    }

    var trimmed = value.Trim();
    return trimmed.Length == 0 ? null : trimmed;
  }

  private static int NormalizeLimit(int? limit)
  {
    if (limit is null || limit <= 0)
    {
      return DefaultLimit;
    }

    return Math.Min(limit.Value, MaxLimit);
  }

  private static string EncodeCursor(DateTime updatedAtUtc, Guid id)
  {
    var raw = $"{updatedAtUtc.Ticks}:{id:D}";
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParseCursor(string? cursor, out DateTime updatedAtUtc, out Guid id)
  {
    updatedAtUtc = default;
    id = Guid.Empty;

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

      if (!Guid.TryParse(parts[1], out id))
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

  private static WishlistDto ToDto(WishlistEntity wishlist, int itemsCount)
  {
    return new WishlistDto(
      wishlist.Id,
      wishlist.Title,
      wishlist.Description,
      wishlist.ThemeId,
      wishlist.UpdatedAtUtc,
      itemsCount);
  }

  private static bool IsForeignKeyConstraint(DbUpdateException ex)
  {
    return ex.InnerException?.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) == true;
  }

  private sealed record WishlistProjection(
    Guid Id,
    string Title,
    string? Description,
    Guid? ThemeId,
    DateTime UpdatedAtUtc);
}
