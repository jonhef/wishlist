using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Wishlist.Api.Api.Auth;

public interface ICurrentUserAccessor
{
  Guid? CurrentUserId { get; }

  Guid GetRequiredUserId();
}

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
  private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

  public Guid? CurrentUserId
  {
    get
    {
      var principal = _httpContextAccessor.HttpContext?.User;
      if (principal is null)
      {
        return null;
      }

      return TryGetUserIdFromClaims(principal, out var userId) ? userId : null;
    }
  }

  public Guid GetRequiredUserId()
  {
    var userId = CurrentUserId;
    if (userId is null)
    {
      throw new InvalidOperationException("Current user id is missing in claims.");
    }

    return userId.Value;
  }

  private static bool TryGetUserIdFromClaims(ClaimsPrincipal principal, out Guid userId)
  {
    userId = Guid.Empty;
    var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

    return !string.IsNullOrWhiteSpace(sub) && Guid.TryParse(sub, out userId);
  }
}
