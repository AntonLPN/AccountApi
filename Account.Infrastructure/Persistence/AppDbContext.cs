using Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.Entity<AppUser>(entity =>
        {
            entity.HasKey(u => u.Id).HasName("PK_AppUser");
            entity.Property(e => e.UserName).HasMaxLength(255).HasColumnName("UserName").IsUnicode();
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode().IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("PasswordHash").IsUnicode()
                .IsRequired();
            entity.Property(e => e.EmailConfirmed).HasColumnName("EmailConfirmed").HasDefaultValue(false);

            entity.HasIndex(u => u.Email).IsUnique();
        });

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
                .HasConstraintName("FK_AppUser_ApiKeys")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}