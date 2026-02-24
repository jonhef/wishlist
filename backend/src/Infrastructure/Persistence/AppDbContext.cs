using Microsoft.EntityFrameworkCore;
using Wishlist.Api.Domain.Entities;

namespace Wishlist.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<WishItem> WishItems => Set<WishItem>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<WishItem>(entity =>
    {
      entity.ToTable("wish_items");

      entity.HasKey(item => item.Id);

      entity.Property(item => item.Title)
        .IsRequired()
        .HasMaxLength(200);

      entity.Property(item => item.CreatedAtUtc)
        .IsRequired();
    });
  }
}
