namespace Wishlist.Api.Api.Auth;

public interface IOwnerResource
{
  Guid OwnerUserId { get; }
}

public sealed record OwnerResource(Guid OwnerUserId) : IOwnerResource;
