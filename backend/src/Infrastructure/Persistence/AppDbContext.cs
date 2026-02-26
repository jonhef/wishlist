using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text;
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
      entity.HasIndex(theme => new { theme.OwnerUserId, theme.CreatedAtUtc, theme.Id });

      entity.Property(theme => theme.Name)
        .IsRequired()
        .HasMaxLength(80);

      entity.Property(theme => theme.TokensJson)
        .HasColumnType("jsonb");

      entity.Property(theme => theme.CreatedAtUtc)
        .IsRequired();
    });

    modelBuilder.Entity<WishlistEntity>(entity =>
    {
      entity.ToTable("wishlists");

      entity.HasKey(wishlist => wishlist.Id);

      entity.HasIndex(wishlist => new { wishlist.OwnerUserId, wishlist.UpdatedAtUtc, wishlist.Id });
      entity.HasIndex(wishlist => wishlist.ShareTokenHash)
        .IsUnique();

      entity.Property(wishlist => wishlist.Title)
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

      entity.Property(wishlist => wishlist.ShareTokenHash)
        .HasMaxLength(64);

      entity.HasOne(wishlist => wishlist.Theme)
        .WithMany(theme => theme.Wishlists)
        .HasForeignKey(wishlist => wishlist.ThemeId)
        .OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<WishItem>(entity =>
    {
      entity.ToTable("wish_items");

      entity.HasKey(item => item.Id);

      entity.HasIndex(item => new { item.WishlistId, item.Priority, item.CreatedAtUtc, item.Id });

      entity.Property(item => item.Name)
        .IsRequired()
        .HasMaxLength(240);

      entity.Property(item => item.Url)
        .HasMaxLength(2048);

      entity.Property(item => item.PriceAmount)
        .HasColumnType("decimal(18,2)");

      entity.Property(item => item.PriceCurrency)
        .HasMaxLength(3);

      entity.Property(item => item.Priority)
        .HasColumnType("numeric(38,18)")
        .IsRequired();

      entity.Property(item => item.Notes)
        .HasMaxLength(2000);

      entity.Property(item => item.CreatedAtUtc)
        .IsRequired();

      entity.Property(item => item.UpdatedAtUtc)
        .IsRequired();

      entity.Property(item => item.IsDeleted)
        .HasDefaultValue(false);

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

    ApplyPostgresConventions(modelBuilder);
  }

  private static void ApplyPostgresConventions(ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      foreach (var property in entityType.GetProperties())
      {
        property.SetColumnName(ToSnakeCase(property.Name));

        var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
        if (clrType == typeof(Guid))
        {
          property.SetColumnType("uuid");
          continue;
        }

        if (clrType == typeof(DateTime))
        {
          property.SetColumnType("timestamp with time zone");
        }
      }
    }
  }

  private static string ToSnakeCase(string value)
  {
    var buffer = new StringBuilder(value.Length + 8);

    for (var i = 0; i < value.Length; i++)
    {
      var character = value[i];
      if (char.IsUpper(character))
      {
        if (i > 0)
        {
          buffer.Append('_');
        }

        buffer.Append(char.ToLowerInvariant(character));
      }
      else
      {
        buffer.Append(character);
      }
    }

    return buffer.ToString();
  }
}
