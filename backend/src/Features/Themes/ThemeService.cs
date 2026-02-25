using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Features.Themes;

public sealed class ThemeService(AppDbContext dbContext, TimeProvider timeProvider) : IThemeService
{
  private const int DefaultLimit = 20;
  private const int MaxLimit = 50;
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private readonly AppDbContext _dbContext = dbContext;
  private readonly TimeProvider _timeProvider = timeProvider;

  public async Task<ThemeServiceResult<ThemeDto>> CreateAsync(
    Guid ownerUserId,
    CreateThemeRequestDto request,
    CancellationToken cancellationToken)
  {
    if (!ValidateName(request.Name) || !ValidateTokens(request.Tokens))
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.ValidationFailed);
    }

    var now = _timeProvider.GetUtcNow().UtcDateTime;
    var entity = new ThemeEntity
    {
      OwnerUserId = ownerUserId,
      Name = request.Name.Trim(),
      TokensJson = JsonSerializer.Serialize(request.Tokens, JsonOptions),
      CreatedAtUtc = now
    };

    _dbContext.Themes.Add(entity);

    try
    {
      await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (IsThemeNameConflict(ex))
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.AlreadyExists);
    }

    return ThemeServiceResult<ThemeDto>.Success(ToDto(entity));
  }

  public async Task<ThemeServiceResult<ThemeListResult>> ListAsync(
    Guid ownerUserId,
    ThemeListQuery query,
    CancellationToken cancellationToken)
  {
    var limit = NormalizeLimit(query.Limit);
    var baseQuery = _dbContext.Themes
      .AsNoTracking()
      .Where(x => x.OwnerUserId == ownerUserId);

    var hasCursor = TryParseCursor(query.Cursor, out var cursorCreatedAt, out var cursorId);
    if (hasCursor)
    {
      baseQuery = baseQuery.Where(x => x.CreatedAtUtc <= cursorCreatedAt);
    }

    var candidates = await baseQuery
      .OrderByDescending(x => x.CreatedAtUtc)
      .ThenByDescending(x => x.Id)
      .Take(limit + 256)
      .ToListAsync(cancellationToken);

    if (hasCursor)
    {
      candidates = candidates
        .Where(x => x.CreatedAtUtc < cursorCreatedAt || (x.CreatedAtUtc == cursorCreatedAt && x.Id.CompareTo(cursorId) < 0))
        .ToList();
    }

    var page = candidates.Take(limit + 1).ToList();
    var hasNext = page.Count > limit;
    if (hasNext)
    {
      page.RemoveAt(page.Count - 1);
    }

    var items = page.Select(ToDto).ToList();
    var nextCursor = hasNext && items.Count > 0
      ? EncodeCursor(items[^1].CreatedAtUtc, items[^1].Id)
      : null;

    return ThemeServiceResult<ThemeListResult>.Success(new ThemeListResult(items, nextCursor));
  }

  public async Task<ThemeServiceResult<ThemeDto>> GetByIdAsync(
    Guid ownerUserId,
    Guid themeId,
    CancellationToken cancellationToken)
  {
    var theme = await _dbContext.Themes
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == themeId, cancellationToken);

    if (theme is null)
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.NotFound);
    }

    if (theme.OwnerUserId != ownerUserId)
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.Forbidden);
    }

    return ThemeServiceResult<ThemeDto>.Success(ToDto(theme));
  }

  public async Task<ThemeServiceResult<ThemeDto>> UpdateAsync(
    Guid ownerUserId,
    Guid themeId,
    UpdateThemeRequestDto request,
    CancellationToken cancellationToken)
  {
    if (request is { Name: null, Tokens: null })
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.ValidationFailed);
    }

    var theme = await _dbContext.Themes
      .FirstOrDefaultAsync(x => x.Id == themeId, cancellationToken);

    if (theme is null)
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.NotFound);
    }

    if (theme.OwnerUserId != ownerUserId)
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.Forbidden);
    }

    if (request.Name is not null)
    {
      if (!ValidateName(request.Name))
      {
        return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.ValidationFailed);
      }

      theme.Name = request.Name.Trim();
    }

    if (request.Tokens is not null)
    {
      if (!ValidateTokens(request.Tokens))
      {
        return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.ValidationFailed);
      }

      theme.TokensJson = JsonSerializer.Serialize(request.Tokens, JsonOptions);
    }

    try
    {
      await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateException ex) when (IsThemeNameConflict(ex))
    {
      return ThemeServiceResult<ThemeDto>.Failure(ThemeErrorCodes.AlreadyExists);
    }

    return ThemeServiceResult<ThemeDto>.Success(ToDto(theme));
  }

  public async Task<ThemeServiceResult<bool>> DeleteAsync(
    Guid ownerUserId,
    Guid themeId,
    CancellationToken cancellationToken)
  {
    var theme = await _dbContext.Themes
      .FirstOrDefaultAsync(x => x.Id == themeId, cancellationToken);

    if (theme is null)
    {
      return ThemeServiceResult<bool>.Failure(ThemeErrorCodes.NotFound);
    }

    if (theme.OwnerUserId != ownerUserId)
    {
      return ThemeServiceResult<bool>.Failure(ThemeErrorCodes.Forbidden);
    }

    _dbContext.Themes.Remove(theme);
    await _dbContext.SaveChangesAsync(cancellationToken);

    return ThemeServiceResult<bool>.Success(true);
  }

  private static ThemeDto ToDto(ThemeEntity entity)
  {
    var tokens = ParseTokensOrDefault(entity.TokensJson);
    return new ThemeDto(entity.Id, entity.Name, tokens, entity.CreatedAtUtc);
  }

  private static ThemeTokensDto ParseTokensOrDefault(string? tokensJson)
  {
    if (!string.IsNullOrWhiteSpace(tokensJson))
    {
      try
      {
        var parsed = JsonSerializer.Deserialize<ThemeTokensDto>(tokensJson, JsonOptions);
        if (parsed is not null && ValidateTokens(parsed))
        {
          return parsed;
        }
      }
      catch
      {
      }
    }

    return DefaultTokens();
  }

  private static bool ValidateName(string name)
  {
    var trimmed = name?.Trim();
    return !string.IsNullOrWhiteSpace(trimmed) && trimmed.Length <= 80;
  }

  private static bool ValidateTokens(ThemeTokensDto? tokens)
  {
    if (tokens is null
      || tokens.Colors is null
      || tokens.Typography is null
      || tokens.Radii is null
      || tokens.Spacing is null)
    {
      return false;
    }

    if (!IsNonEmpty(tokens.Colors.Bg)
      || !IsNonEmpty(tokens.Colors.Text)
      || !IsNonEmpty(tokens.Colors.Primary)
      || !IsNonEmpty(tokens.Colors.Secondary)
      || !IsNonEmpty(tokens.Colors.Muted)
      || !IsNonEmpty(tokens.Colors.Border))
    {
      return false;
    }

    if (!IsNonEmpty(tokens.Typography.FontFamily)
      || tokens.Typography.FontSizeBase <= 0)
    {
      return false;
    }

    if (tokens.Radii.Sm < 0 || tokens.Radii.Md < 0 || tokens.Radii.Lg < 0)
    {
      return false;
    }

    if (tokens.Spacing.Xs < 0 || tokens.Spacing.Sm < 0 || tokens.Spacing.Md < 0 || tokens.Spacing.Lg < 0)
    {
      return false;
    }

    return true;
  }

  private static bool IsNonEmpty(string? value)
  {
    return !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 120;
  }

  private static int NormalizeLimit(int? limit)
  {
    if (limit is null || limit <= 0)
    {
      return DefaultLimit;
    }

    return Math.Min(limit.Value, MaxLimit);
  }

  private static string EncodeCursor(DateTime createdAtUtc, Guid id)
  {
    var raw = $"{createdAtUtc.Ticks}:{id:D}";
    return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
  }

  private static bool TryParseCursor(string? cursor, out DateTime createdAtUtc, out Guid id)
  {
    createdAtUtc = default;
    id = default;

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

      createdAtUtc = new DateTime(ticks, DateTimeKind.Utc);
      return true;
    }
    catch
    {
      return false;
    }
  }

  private static bool IsThemeNameConflict(DbUpdateException ex)
  {
    var message = ex.InnerException?.Message ?? ex.Message;
    return message.Contains("themes.OwnerUserId, themes.Name", StringComparison.OrdinalIgnoreCase);
  }

  private static ThemeTokensDto DefaultTokens()
  {
    return new ThemeTokensDto(
      new ThemeColorsDto("#ffffff", "#111111", "#0d6efd", "#6c757d", "#f8f9fa", "#dee2e6"),
      new ThemeTypographyDto("system-ui", 16),
      new ThemeRadiiDto(4, 8, 12),
      new ThemeSpacingDto(4, 8, 12, 16));
  }
}
