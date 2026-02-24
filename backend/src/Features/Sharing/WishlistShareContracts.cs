namespace Wishlist.Api.Features.Sharing;

public sealed record ShareRotationResult(string Token);

public sealed record PublicWishlistItemDto(
  string Name,
  string? Url,
  decimal? PriceAmount,
  string? PriceCurrency,
  int Priority,
  string? Notes);

public sealed record PublicWishlistDto(
  string Title,
  string? Description,
  IReadOnlyList<PublicWishlistItemDto> Items);

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
}
