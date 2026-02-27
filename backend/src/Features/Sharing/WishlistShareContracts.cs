using Wishlist.Api.Features.Themes;

namespace Wishlist.Api.Features.Sharing;

public sealed record ShareRotationResult(string Token);

public enum PublicWishlistSort
{
  priority = 0,
  added = 1,
  price = 2
}

public enum PublicWishlistOrder
{
  asc = 0,
  desc = 1
}

public sealed record PublicWishlistListQuery(
  string? Cursor,
  int? Limit,
  PublicWishlistSort Sort = PublicWishlistSort.priority,
  PublicWishlistOrder Order = PublicWishlistOrder.asc);

public sealed record PublicWishlistItemDto(
  int Id,
  string Name,
  string? Url,
  int? PriceAmount,
  string? PriceCurrency,
  string? Notes,
  DateTime CreatedAt);

public sealed record PublicWishlistDto(
  string Title,
  string? Description,
  ThemeTokensDto ThemeTokens,
  IReadOnlyList<PublicWishlistItemDto> Items,
  string? NextCursor);

public sealed record WishlistShareServiceResult<T>(T? Value, string? ErrorCode)
{
  public bool IsSuccess => ErrorCode is null;

  public static WishlistShareServiceResult<T> Success(T value) => new(value, null);

  public static WishlistShareServiceResult<T> Failure(string errorCode) => new(default, errorCode);
}

public static class WishlistShareErrorCodes
{
  public const string NotFound = "not_found";
  public const string Forbidden = "forbidden";
  public const string FxUnavailable = "fx_unavailable";
}
