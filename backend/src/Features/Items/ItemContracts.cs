namespace Wishlist.Api.Features.Items;

public sealed record CreateItemRequestDto(
  string Name,
  string? Url,
  decimal? PriceAmount,
  string? PriceCurrency,
  int Priority,
  string? Notes);

public sealed record UpdateItemRequestDto(
  string? Name,
  string? Url,
  decimal? PriceAmount,
  string? PriceCurrency,
  int? Priority,
  string? Notes);

public sealed record ItemListQuery(string? Cursor, int? Limit);

public sealed record ItemDto(
  int Id,
  Guid WishlistId,
  string Name,
  string? Url,
  decimal? PriceAmount,
  string? PriceCurrency,
  int Priority,
  string? Notes,
  DateTime UpdatedAtUtc);

public sealed record ItemListResult(IReadOnlyList<ItemDto> Items, string? NextCursor);

public sealed record ItemServiceResult<T>(T? Value, string? ErrorCode)
{
  public bool IsSuccess => ErrorCode is null;

  public static ItemServiceResult<T> Success(T value) => new(value, null);

  public static ItemServiceResult<T> Failure(string errorCode) => new(default, errorCode);
}

public static class ItemErrorCodes
{
  public const string ValidationFailed = "validation_failed";
  public const string NotFound = "not_found";
  public const string Forbidden = "forbidden";
}
