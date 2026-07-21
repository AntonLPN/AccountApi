using Account.Domain.Entities;
using Account.Infrastructure.Persistence.SagaModels;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Account.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<LoginAudit> LoginAudits { get; set; } = null!;
    public DbSet<LogoutAudit> LogoutAudits { get; set; } = null!;

    public DbSet<OtpSessions> OptSessions { get; set; } = null!;

    //Sagas
    public DbSet<UserRegistrationSagaState> UserRegistrationSagaStates { get; set; } = null!;
    public DbSet<UserLoginSagaState> UserLoginSagaStates { get; set; } = null!;
    public DbSet<UserLogoutSagaState> UserLogoutSagaStates { get; set; } = null!;
    public DbSet<TwoFactorSagaState> TwoFactorSagaStates { get; set; } = null!;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //MassTransit Outbox
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(e => e.Id).IsRequired()
                .ValueGeneratedNever();
            entity.HasKey(u => u.Id).HasName("PK_AppUser");
            entity.Property(e => e.UserName).HasMaxLength(255).HasColumnName("UserName").IsUnicode();
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode().IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").IsUnicode()
                .IsRequired();
            entity.Property(e => e.EmailConfirmed).HasColumnName("EmailConfirmed").HasDefaultValue(false);
            entity.Property(e => e.IsTwoFactorEnabled).HasColumnName("IsTwoFactorEnabled").HasDefaultValue(false);
            entity.Property(e => e.EncryptedTwoFactorSecret).HasColumnName("EncryptedTwoFactorSecret").IsUnicode();
            entity.Property(e => e.ReferralCode).HasMaxLength(255).HasColumnName("ReferralCode").IsUnicode()
                .IsRequired();
            entity.Property(e => e.ReferrerId).HasMaxLength(255).HasColumnName("ReferrerId").IsUnicode();
            entity.Property(e => e.ProviderName).HasMaxLength(60).HasColumnName("ProviderName").IsUnicode();
            entity.Property(e => e.LastLoginAt).HasColumnName("LastLoginAt");
            entity.Property(e => e.LastLogoutAt).HasColumnName("LastLogoutAt");

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
                .HasConstraintName("FK_AppUser_ApiKeys");
            
            entity.HasIndex(a => a.ApiKeyValue)
                .IsUnique()
                .HasDatabaseName("UX_ApiKey_Key");
        });

        builder.Entity<UserRegistrationSagaState>(entity =>
        {
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CorrelationId).HasMaxLength(255);
            entity.Property(s => s.CurrentState).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(255);
            entity.Property(x => x.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(x => x.ApiKey).HasMaxLength(255).HasColumnName("ApiKey").IsUnicode();
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            entity.Property(x => x.EmailConfirmationSent).HasColumnName("EmailConfirmationSent").HasDefaultValue(false);
            entity.Property(x => x.ProfileInitialized).HasColumnName("ProfileInitialized").HasDefaultValue(false);
            entity.Property(x => x.FailureReason).HasMaxLength(255).HasColumnName("FailureReason").IsUnicode();
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<UserLoginSagaState>(entity =>
        {
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CorrelationId).HasMaxLength(255);
            entity.Property(s => s.CurrentState).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(255);
            entity.Property(x => x.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(x => x.IpAddress).HasMaxLength(64).HasColumnName("IpAddress").IsUnicode();
            entity.Property(x => x.UserAgent).HasMaxLength(512).HasColumnName("UserAgent").IsUnicode();
            entity.Property(x => x.IsSuspicious).HasColumnName("IsSuspicious").HasDefaultValue(false);
            entity.Property(x => x.AuditRecorded).HasColumnName("AuditRecorded").HasDefaultValue(false);
            entity.Property(x => x.LastLoginUpdated).HasColumnName("LastLoginUpdated").HasDefaultValue(false);
            entity.Property(x => x.NotificationSent).HasColumnName("NotificationSent").HasDefaultValue(false);
            entity.Property(x => x.FailureReason).HasMaxLength(255).HasColumnName("FailureReason").IsUnicode();
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<LoginAudit>(entity =>
        {
            entity.HasKey(a => a.Id).HasName("PK_LoginAudit");
            entity.Property(a => a.UserId).HasMaxLength(255).HasColumnName("UserId").IsUnicode();
            entity.Property(a => a.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(a => a.IpAddress).HasMaxLength(64).HasColumnName("IpAddress").IsUnicode();
            entity.Property(a => a.UserAgent).HasMaxLength(512).HasColumnName("UserAgent").IsUnicode();
            entity.Property(a => a.IsSuspicious).HasColumnName("IsSuspicious");
            entity.Property(a => a.LoggedInAt).HasColumnName("LoggedInAt");
            entity.HasIndex(a => a.UserId);
        });

        builder.Entity<UserLogoutSagaState>(entity =>
        {
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CorrelationId).HasMaxLength(255);
            entity.Property(s => s.CurrentState).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(255);
            entity.Property(x => x.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(x => x.IpAddress).HasMaxLength(64).HasColumnName("IpAddress").IsUnicode();
            entity.Property(x => x.UserAgent).HasMaxLength(512).HasColumnName("UserAgent").IsUnicode();
            entity.Property(x => x.AuditRecorded).HasColumnName("AuditRecorded").HasDefaultValue(false);
            entity.Property(x => x.LastLogoutUpdated).HasColumnName("LastLogoutUpdated").HasDefaultValue(false);
            entity.Property(x => x.NotificationSent).HasColumnName("NotificationSent").HasDefaultValue(false);
            entity.Property(x => x.FailureReason).HasMaxLength(255).HasColumnName("FailureReason").IsUnicode();
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<TwoFactorSagaState>(entity =>
        {
            entity.HasKey(s => s.CorrelationId);
            entity.Property(s => s.CorrelationId).HasMaxLength(255);
            entity.Property(s => s.CurrentState).HasMaxLength(64);
            entity.Property(x => x.UserId).HasMaxLength(255);
            entity.Property(x => x.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(x => x.OtpCode).HasMaxLength(7).HasColumnName("OtpCode").IsUnicode();
            entity.Property(x => x.OtpCodeSent).HasColumnName("OtpCodeSent").HasDefaultValue(false);
            entity.Property(x => x.FailureReason).HasMaxLength(255).HasColumnName("FailureReason").IsUnicode();
            entity.Property(x => x.ExpiredAt).HasColumnName("ExpiredAt");
            entity.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<LogoutAudit>(entity =>
        {
            entity.HasKey(a => a.Id).HasName("PK_LogoutAudit");
            entity.Property(a => a.UserId).HasMaxLength(255).HasColumnName("UserId").IsUnicode();
            entity.Property(a => a.Email).HasMaxLength(255).HasColumnName("Email").IsUnicode();
            entity.Property(a => a.IpAddress).HasMaxLength(64).HasColumnName("IpAddress").IsUnicode();
            entity.Property(a => a.UserAgent).HasMaxLength(512).HasColumnName("UserAgent").IsUnicode();
            entity.Property(a => a.LoggedOutAt).HasColumnName("LoggedOutAt");
            entity.HasIndex(a => a.UserId);
        });
        
        builder.Entity<OtpSessions>(entity =>
        {
            entity.HasKey(a => a.Id).HasName("PK_OptSessions");
            entity.Property(a => a.CodeHash).HasMaxLength(255).HasColumnName("CodeHash").IsUnicode();
            entity.Property(a => a.CorrelationId).HasMaxLength(255).HasColumnName("CorrelationId").IsUnicode();
            entity.Property(a => a.UserId).HasMaxLength(255).HasColumnName("UserId").IsUnicode();
            entity.Property(a => a.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(a => a.ExpiresAt).HasColumnName("ExpiresAt");
            entity.Property(a => a.UsedAt).HasColumnName("UsedAt");
            
            entity.HasIndex(a => a.CorrelationId)
                .IsUnique()
                .HasDatabaseName("UX_OtpSessions_CorrelationId");
            
            entity.HasIndex(a => a.UserId)
                .HasDatabaseName("UX_OtpSessions_ActiveUserId");
        });
    }
}