using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class ThemeServiceTests
{
  [Fact]
  public async Task CreateAsync_InvalidTokens_ReturnsValidationFailed()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);
    var owner = CreateUser("theme-owner1@example.com");

    dbContext.Users.Add(owner);
    await dbContext.SaveChangesAsync();

    var invalidTokens = new ThemeTokensDto(
      new ThemeColorsDto("", "#111", "#222", "#333", "#444", "#555"),
      new ThemeTypographyDto("Manrope", 16),
      new ThemeRadiiDto(2, 4, 8),
      new ThemeSpacingDto(4, 8, 12, 16));

    var result = await service.CreateAsync(
      owner.Id,
      new CreateThemeRequestDto("Invalid", invalidTokens),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ThemeErrorCodes.ValidationFailed, result.ErrorCode);
  }

  [Fact]
  public async Task ListAsync_ReturnsOnlyOwnerThemes()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var owner = CreateUser("theme-owner2@example.com");
    var other = CreateUser("theme-other2@example.com");

    dbContext.Users.AddRange(owner, other);
    dbContext.Themes.AddRange(
      CreateTheme(owner.Id, "Owner 1", DefaultTokens(), DateTime.UtcNow.AddMinutes(-2)),
      CreateTheme(owner.Id, "Owner 2", DefaultTokens(), DateTime.UtcNow.AddMinutes(-1)),
      CreateTheme(other.Id, "Other 1", DefaultTokens(), DateTime.UtcNow));
    await dbContext.SaveChangesAsync();

    var result = await service.ListAsync(owner.Id, new ThemeListQuery(null, 50), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(2, result.Value!.Items.Count);
    Assert.All(result.Value.Items, x => Assert.StartsWith("Owner", x.Name));
  }

  [Fact]
  public async Task UpdateAsync_ReplacesWholeTokens()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);
    var owner = CreateUser("theme-owner3@example.com");
    var theme = CreateTheme(owner.Id, "Old", DefaultTokens(), DateTime.UtcNow);

    dbContext.Users.Add(owner);
    dbContext.Themes.Add(theme);
    await dbContext.SaveChangesAsync();

    var replacementTokens = new ThemeTokensDto(
      new ThemeColorsDto("#000000", "#ffffff", "#ff0000", "#00ff00", "#999999", "#444444"),
      new ThemeTypographyDto("IBM Plex Sans", 15),
      new ThemeRadiiDto(3, 6, 9),
      new ThemeSpacingDto(2, 6, 10, 14));

    var result = await service.UpdateAsync(
      owner.Id,
      theme.Id,
      new UpdateThemeRequestDto(null, replacementTokens),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("#000000", result.Value!.Tokens.Colors.Bg);
    Assert.Equal("IBM Plex Sans", result.Value.Tokens.Typography.FontFamily);
    Assert.Equal(14, result.Value.Tokens.Spacing.Lg);
  }

  [Fact]
  public async Task GetByIdAsync_ForeignTheme_ReturnsForbidden()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var owner = CreateUser("theme-owner4@example.com");
    var intruder = CreateUser("theme-intruder4@example.com");
    var theme = CreateTheme(owner.Id, "Private", DefaultTokens(), DateTime.UtcNow);

    dbContext.Users.AddRange(owner, intruder);
    dbContext.Themes.Add(theme);
    await dbContext.SaveChangesAsync();

    var result = await service.GetByIdAsync(intruder.Id, theme.Id, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ThemeErrorCodes.Forbidden, result.ErrorCode);
  }

  [Fact]
  public async Task CreateAsync_DuplicateNameForOwner_ReturnsAlreadyExists()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var owner = CreateUser("theme-owner5@example.com");
    dbContext.Users.Add(owner);
    dbContext.Themes.Add(CreateTheme(owner.Id, "Dup", DefaultTokens(), DateTime.UtcNow));
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      new CreateThemeRequestDto("Dup", DefaultTokens()),
      CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Equal(ThemeErrorCodes.AlreadyExists, result.ErrorCode);
  }

  private static AppDbContext CreateDbContext()
  {
    var options = new DbContextOptionsBuilder<AppDbContext>()
      .UseSqlite("Filename=:memory:")
      .Options;

    var dbContext = new AppDbContext(options);
    dbContext.Database.OpenConnection();
    dbContext.Database.EnsureCreated();

    return dbContext;
  }

  private static AppUser CreateUser(string email)
  {
    var user = new AppUser
    {
      Email = email,
      NormalizedEmail = email.ToUpperInvariant(),
      CreatedAtUtc = DateTime.UtcNow
    };

    user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, "VeryStrongPass123");
    return user;
  }

  private static ThemeEntity CreateTheme(Guid ownerId, string name, ThemeTokensDto tokens, DateTime createdAtUtc)
  {
    return new ThemeEntity
    {
      OwnerUserId = ownerId,
      Name = name,
      TokensJson = System.Text.Json.JsonSerializer.Serialize(tokens, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)),
      CreatedAtUtc = createdAtUtc
    };
  }

  private static ThemeTokensDto DefaultTokens()
  {
    return new ThemeTokensDto(
      new ThemeColorsDto("#ffffff", "#111111", "#0d6efd", "#6c757d", "#f8f9fa", "#dee2e6"),
      new ThemeTypographyDto("Inter", 16),
      new ThemeRadiiDto(4, 8, 12),
      new ThemeSpacingDto(4, 8, 12, 16));
  }
}
