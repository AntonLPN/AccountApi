using Account.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(a => a.Id).HasName("PK_ApiKey");
            entity.Property(e => e.UserId).HasMaxLength(255).HasColumnName("UserId").IsUnicode();
            entity.Property(e => e.ApiKeyValue).HasMaxLength(255).HasColumnName("Key").IsUnicode();
            entity.Property(e => e.IsAuthorize).HasColumnName("Authorize").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.ExpiredAt).HasColumnName("ExpiredAt");

            entity.HasOne(a => a.AppUser)
                .WithMany(u => u.ApiKeys)
                .HasForeignKey(a => a.UserId)
                .HasConstraintName("FK_ApiKey_AppUser");
        });
    }
}