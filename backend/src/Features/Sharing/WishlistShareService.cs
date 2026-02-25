using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Sharing;

public sealed class WishlistShareService(AppDbContext dbContext) : IWishlistShareService
{
  private const int DefaultLimit = 20;
  private const int MaxLimit = 50;

  private readonly AppDbContext _dbContext = dbContext;

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

    var limit = NormalizeLimit(query.Limit);
    var itemsQuery = _dbContext.WishItems
      .AsNoTracking()
      .Where(x => x.WishlistId == wishlist.Id && !x.IsDeleted)
      .OrderByDescending(x => x.UpdatedAtUtc)
      .ThenByDescending(x => x.Id);

    var hasCursor = TryParseCursor(query.Cursor, out var cursorUpdatedAt, out var cursorId);
    if (hasCursor)
    {
      itemsQuery = itemsQuery.Where(item =>
        item.UpdatedAtUtc < cursorUpdatedAt
        || (item.UpdatedAtUtc == cursorUpdatedAt && item.Id < cursorId))
      .OrderByDescending(x => x.UpdatedAtUtc)
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
        x.UpdatedAtUtc,
        x.Id))
      .ToListAsync(cancellationToken);

    var hasNext = candidates.Count > limit;
    if (hasNext)
    {
      candidates.RemoveAt(candidates.Count - 1);
    }

    var page = candidates
      .Select(x => new PublicWishlistItemDto(
        x.Name,
        x.Url,
        x.PriceAmount,
        x.PriceCurrency,
        x.Priority,
        x.Notes))
      .ToList();

    var nextCursor = hasNext && candidates.Count > 0
      ? EncodeCursor(candidates[^1].UpdatedAtUtc, candidates[^1].Id)
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

  private sealed record PublicWishlistItemProjection(
    string Name,
    string? Url,
    decimal? PriceAmount,
    string? PriceCurrency,
    int Priority,
    string? Notes,
    DateTime UpdatedAtUtc,
    int Id);
}
