using Microsoft.AspNetCore.Authorization;

namespace Wishlist.Api.Api.Auth;

public sealed class OwnerAuthorizationHandler(ICurrentUserAccessor currentUserAccessor)
  : AuthorizationHandler<OwnerRequirement, IOwnerResource>
{
  private readonly ICurrentUserAccessor _currentUserAccessor = currentUserAccessor;

  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    OwnerRequirement requirement,
    IOwnerResource resource)
  {
    var currentUserId = _currentUserAccessor.CurrentUserId;

    if (currentUserId.HasValue && currentUserId.Value == resource.OwnerUserId)
    {
      context.Succeed(requirement);
    }

    return Task.CompletedTask;
  }
}
