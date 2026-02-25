using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;
using Wishlist.Api.Features.Themes;
using Wishlist.Api.Infrastructure.Persistence;

namespace Wishlist.Api.Tests;

public sealed class ThemeServiceTests
{
  [Fact]
  public async Task GetDefaultAsync_ReturnsDefaultDarkPinkNeonPreset()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var result = await service.GetDefaultAsync(CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("DefaultDarkPinkNeon", result.Value!.Name);
    Assert.Equal(1, result.Value.Tokens.SchemaVersion);
    Assert.Equal("#0b0610", result.Value.Tokens.Colors.Bg0);
  }

  [Fact]
  public async Task CreateAsync_InvalidTokens_ReturnsValidationFailed()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);
    var owner = CreateUser("theme-owner1@example.com");

    dbContext.Users.Add(owner);
    await dbContext.SaveChangesAsync();

    var invalidTokens = new ThemeTokensPatchDto(
      1,
      new ThemeColorsPatchDto(null, null, null, null, null, null, null, null, null, null, null, null, null, null),
      null,
      null,
      null);

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
      CreateTheme(owner.Id, "Owner 1", DefaultTokensPatch(), DateTime.UtcNow.AddMinutes(-2)),
      CreateTheme(owner.Id, "Owner 2", DefaultTokensPatch(), DateTime.UtcNow.AddMinutes(-1)),
      CreateTheme(other.Id, "Other 1", DefaultTokensPatch(), DateTime.UtcNow));
    await dbContext.SaveChangesAsync();

    var result = await service.ListAsync(owner.Id, new ThemeListQuery(null, 50), CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal(2, result.Value!.Items.Count);
    Assert.All(result.Value.Items, x => Assert.StartsWith("Owner", x.Name));
  }

  [Fact]
  public async Task ListAsync_LimitAboveMax_ReturnsTwoPagesWithMax50()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var owner = CreateUser("theme-owner-pagination@example.com");
    dbContext.Users.Add(owner);

    var baseTime = DateTime.UtcNow;
    for (var i = 0; i < 55; i++)
    {
      dbContext.Themes.Add(CreateTheme(owner.Id, $"Theme-{i}", DefaultTokensPatch(), baseTime.AddMinutes(i)));
    }

    await dbContext.SaveChangesAsync();

    var firstPage = await service.ListAsync(owner.Id, new ThemeListQuery(null, 500), CancellationToken.None);
    Assert.True(firstPage.IsSuccess);
    Assert.NotNull(firstPage.Value);
    Assert.Equal(50, firstPage.Value!.Items.Count);
    Assert.NotNull(firstPage.Value.NextCursor);

    var secondPage = await service.ListAsync(
      owner.Id,
      new ThemeListQuery(firstPage.Value.NextCursor, 500),
      CancellationToken.None);

    Assert.True(secondPage.IsSuccess);
    Assert.NotNull(secondPage.Value);
    Assert.Equal(5, secondPage.Value!.Items.Count);
    Assert.Null(secondPage.Value.NextCursor);

    var allIds = firstPage.Value.Items.Select(x => x.Id).Concat(secondPage.Value.Items.Select(x => x.Id)).ToList();
    Assert.Equal(55, allIds.Distinct().Count());
  }

  [Fact]
  public async Task UpdateAsync_IncompleteTokens_AppliesDefaultFallback()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);
    var owner = CreateUser("theme-owner3@example.com");
    var theme = CreateTheme(owner.Id, "Old", DefaultTokensPatch(), DateTime.UtcNow);

    dbContext.Users.Add(owner);
    dbContext.Themes.Add(theme);
    await dbContext.SaveChangesAsync();

    var replacementTokens = new ThemeTokensPatchDto(
      1,
      new ThemeColorsPatchDto("#000000", null, null, "#ffffff", null, null, "#ff0000", null, null, null, null, null, null, null),
      new ThemeTypographyPatchDto("\"Unbounded\", sans-serif", null, null, -0.04m, null),
      new ThemeRadiiPatchDto(6, null, null),
      null);

    var result = await service.UpdateAsync(
      owner.Id,
      theme.Id,
      new UpdateThemeRequestDto(null, replacementTokens),
      CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotNull(result.Value);
    Assert.Equal("#000000", result.Value!.Tokens.Colors.Bg0);
    Assert.Equal("#ffffff", result.Value.Tokens.Colors.Text);
    Assert.Equal("#120a1a", result.Value.Tokens.Colors.Bg1);
    Assert.Equal(16, result.Value.Tokens.Radii.Md);
    Assert.Equal(1, result.Value.Tokens.SchemaVersion);
  }

  [Fact]
  public async Task GetByIdAsync_ForeignTheme_ReturnsForbidden()
  {
    await using var dbContext = CreateDbContext();
    var service = new ThemeService(dbContext, TimeProvider.System);

    var owner = CreateUser("theme-owner4@example.com");
    var intruder = CreateUser("theme-intruder4@example.com");
    var theme = CreateTheme(owner.Id, "Private", DefaultTokensPatch(), DateTime.UtcNow);

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
    dbContext.Themes.Add(CreateTheme(owner.Id, "Dup", DefaultTokensPatch(), DateTime.UtcNow));
    await dbContext.SaveChangesAsync();

    var result = await service.CreateAsync(
      owner.Id,
      new CreateThemeRequestDto("Dup", DefaultTokensPatch()),
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

  private static ThemeEntity CreateTheme(Guid ownerId, string name, ThemeTokensPatchDto tokens, DateTime createdAtUtc)
  {
    return new ThemeEntity
    {
      OwnerUserId = ownerId,
      Name = name,
      TokensJson = JsonSerializer.Serialize(tokens, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
      CreatedAtUtc = createdAtUtc
    };
  }

  private static ThemeTokensPatchDto DefaultTokensPatch()
  {
    var defaults = ThemeTokenDefaults.CreateDefault();

    return new ThemeTokensPatchDto(
      defaults.SchemaVersion,
      new ThemeColorsPatchDto(
        defaults.Colors.Bg0,
        defaults.Colors.Bg1,
        defaults.Colors.Bg2,
        defaults.Colors.Text,
        defaults.Colors.MutedText,
        defaults.Colors.Border,
        defaults.Colors.Primary,
        defaults.Colors.PrimaryHover,
        defaults.Colors.AccentNeon,
        defaults.Colors.Secondary,
        defaults.Colors.Danger,
        defaults.Colors.Success,
        defaults.Colors.Warn,
        defaults.Colors.Error),
      new ThemeTypographyPatchDto(
        defaults.Typography.FontDisplay,
        defaults.Typography.FontBody,
        defaults.Typography.FontMono,
        defaults.Typography.LetterSpacingDisplay,
        defaults.Typography.DisplayFontEnabled),
      new ThemeRadiiPatchDto(
        defaults.Radii.Sm,
        defaults.Radii.Md,
        defaults.Radii.Lg),
      new ThemeEffectsPatchDto(
        defaults.Effects.GlowSm,
        defaults.Effects.GlowMd,
        defaults.Effects.GlowLg,
        defaults.Effects.GlowEnabled,
        defaults.Effects.GlowIntensity,
        defaults.Effects.NoiseOpacity));
  }
}
