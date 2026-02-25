namespace Wishlist.Api.Features.Themes;

public interface IThemeService
{
  Task<ThemeServiceResult<DefaultThemeDto>> GetDefaultAsync(CancellationToken cancellationToken);

  Task<ThemeServiceResult<ThemeDto>> CreateAsync(
    Guid ownerUserId,
    CreateThemeRequestDto request,
    CancellationToken cancellationToken);

  Task<ThemeServiceResult<ThemeListResult>> ListAsync(
    Guid ownerUserId,
    ThemeListQuery query,
    CancellationToken cancellationToken);

  Task<ThemeServiceResult<ThemeDto>> GetByIdAsync(
    Guid ownerUserId,
    Guid themeId,
    CancellationToken cancellationToken);

  Task<ThemeServiceResult<ThemeDto>> UpdateAsync(
    Guid ownerUserId,
    Guid themeId,
    UpdateThemeRequestDto request,
    CancellationToken cancellationToken);

  Task<ThemeServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid themeId,
    CancellationToken cancellationToken);
}
