using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<ThemeEntity> Themes => Set<ThemeEntity>();
  public DbSet<WishlistEntity> Wishlists => Set<WishlistEntity>();
  public DbSet<WishItem> WishItems => Set<WishItem>();
  public DbSet<AppUser> Users => Set<AppUser>();
  public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<ThemeEntity>(entity =>
    {
      entity.ToTable("themes");

      entity.HasKey(theme => theme.Id);

      entity.HasIndex(theme => new { theme.OwnerUserId, theme.Name })
        .IsUnique();

      entity.Property(theme => theme.Name)
        .IsRequired()
        .HasMaxLength(80);

      entity.Property(theme => theme.CreatedAtUtc)
        .IsRequired();
    });

    modelBuilder.Entity<WishlistEntity>(entity =>
    {
      entity.ToTable("wishlists");

      entity.HasKey(wishlist => wishlist.Id);

      entity.HasIndex(wishlist => new { wishlist.OwnerUserId, wishlist.UpdatedAtUtc, wishlist.Id });

      entity.Property(wishlist => wishlist.Title)
        .HasColumnName("Name")
        .IsRequired()
        .HasMaxLength(120);

      entity.Property(wishlist => wishlist.Description)
        .HasMaxLength(1000);

      entity.Property(wishlist => wishlist.CreatedAtUtc)
        .IsRequired();

      entity.Property(wishlist => wishlist.UpdatedAtUtc)
        .IsRequired();

      entity.Property(wishlist => wishlist.IsDeleted)
        .HasDefaultValue(false);

      entity.HasOne(wishlist => wishlist.Theme)
        .WithMany(theme => theme.Wishlists)
        .HasForeignKey(wishlist => wishlist.ThemeId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<WishItem>(entity =>
    {
      entity.ToTable("wish_items");

      entity.HasKey(item => item.Id);

      entity.HasIndex(item => new { item.WishlistId, item.Id });

      entity.Property(item => item.Title)
        .IsRequired()
        .HasMaxLength(200);

      entity.Property(item => item.CreatedAtUtc)
        .IsRequired();

      entity.HasOne(item => item.Wishlist)
        .WithMany(wishlist => wishlist.Items)
        .HasForeignKey(item => item.WishlistId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<AppUser>(entity =>
    {
      entity.ToTable("users");

      entity.HasKey(user => user.Id);

      entity.HasIndex(user => user.NormalizedEmail)
        .IsUnique();

      entity.Property(user => user.Email)
        .IsRequired()
        .HasMaxLength(320);

      entity.Property(user => user.NormalizedEmail)
        .IsRequired()
        .HasMaxLength(320);

      entity.Property(user => user.PasswordHash)
        .IsRequired();

      entity.Property(user => user.CreatedAtUtc)
        .IsRequired();

      entity.HasMany(user => user.Wishlists)
        .WithOne(wishlist => wishlist.OwnerUser)
        .HasForeignKey(wishlist => wishlist.OwnerUserId)
        .OnDelete(DeleteBehavior.Cascade);

      entity.HasMany(user => user.Themes)
        .WithOne(theme => theme.OwnerUser)
        .HasForeignKey(theme => theme.OwnerUserId)
        .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<RefreshToken>(entity =>
    {
      entity.ToTable("refresh_tokens");

      entity.HasKey(token => token.Id);

      entity.HasIndex(token => token.Jti)
        .IsUnique();

      entity.HasIndex(token => new { token.UserId, token.FamilyId });

      entity.Property(token => token.TokenHash)
        .IsRequired();

      entity.Property(token => token.CreatedByIp)
        .HasMaxLength(64);

      entity.Property(token => token.UserAgent)
        .HasMaxLength(512);

      entity.Property(token => token.CreatedAtUtc)
        .IsRequired();

      entity.Property(token => token.ExpiresAtUtc)
        .IsRequired();

      entity.HasOne(token => token.User)
        .WithMany(user => user.RefreshTokens)
        .HasForeignKey(token => token.UserId)
        .OnDelete(DeleteBehavior.Cascade);
    });
  }
}
