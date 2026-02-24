namespace Wishlist.Api.Features.Wishlists;

public sealed record CreateWishlistRequestDto(string Title, string? Description, Guid? ThemeId);

public sealed record UpdateWishlistRequestDto(string? Title, string? Description, Guid? ThemeId);

public sealed record WishlistListQuery(string? Cursor, int? Limit);

public sealed record WishlistDto(
  Guid Id,
  string Title,
  string? Description,
  Guid? ThemeId,
  DateTime UpdatedAt,
  int ItemsCount);

public sealed record WishlistListResult(IReadOnlyList<WishlistDto> Items, string? NextCursor);

public sealed record WishlistServiceResult<T>(T? Value, string? ErrorCode)
{
  public bool IsSuccess => ErrorCode is null;

  public static WishlistServiceResult<T> Success(T value) => new(value, null);

  public static WishlistServiceResult<T> Failure(string errorCode) => new(default, errorCode);
}

public static class WishlistErrorCodes
{
  public const string ValidationFailed = "validation_failed";
  public const string NotFound = "not_found";
  public const string Forbidden = "forbidden";
  public const string ThemeNotAccessible = "theme_not_accessible";
}
