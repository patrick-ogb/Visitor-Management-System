using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VMS.Core.Entities;

namespace VMS.Infrastructure.Data;

public class VmsDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public VmsDbContext(DbContextOptions<VmsDbContext> options) : base(options)
    {
    }

    // DbSets for custom entities
    public DbSet<Enterprise> Enterprises { get; set; }
    public DbSet<EnterpriseUser> EnterpriseUsers { get; set; }
    public DbSet<Guest> Guests { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<GuestInvitation> GuestInvitations { get; set; }
    public DbSet<Approval> Approvals { get; set; }
    public DbSet<NotificationEvent> NotificationEvents { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserPresence> UserPresences { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EnterpriseId)
                .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Index for EnterpriseId (no foreign key constraint - EnterpriseId can reference OnePortal enterprises)
            entity.HasIndex(e => e.EnterpriseId);
        });

        // Configure Enterprise
        builder.Entity<Enterprise>(entity =>
        {
            entity.HasKey(e => e.EnterpriseId);

            entity.Property(e => e.Code)
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.OnePortalId);
        });

        // Configure EnterpriseUser
        builder.Entity<EnterpriseUser>(entity =>
        {
            entity.HasKey(e => e.EnterpriseUserId);

            entity.Property(e => e.EmailAddress)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.RoleName)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(e => e.Enterprise)
                .WithMany()
                .HasForeignKey(e => e.EnterpriseId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => e.OnePortalUserId);
            entity.HasIndex(e => e.OnePortalEnterpriseId);
            entity.HasIndex(e => e.EmailAddress);
            
            // Unique constraint on OnePortalUserId + OnePortalEnterpriseId
            entity.HasIndex(e => new { e.OnePortalUserId, e.OnePortalEnterpriseId })
                .IsUnique();
        });

        // Configure Guest
        builder.Entity<Guest>(entity =>
        {
            entity.HasKey(g => g.GuestId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        // Configure Vehicle
        builder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(v => v.VehicleId);

            entity.Property(e => e.PlateNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Model)
                .HasMaxLength(100);

            entity.Property(e => e.Color)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Relationship to Guest (optional, for backward compatibility)
            // Using NoAction to avoid cascade path conflicts with GuestInvitation -> Guest cascade
            entity.HasOne(v => v.Guest)
                .WithMany(g => g.Vehicles)
                .HasForeignKey(v => v.GuestId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Relationship to GuestInvitation (primary relationship for invitations)
            entity.HasOne(v => v.GuestInvitation)
                .WithMany(gi => gi.Vehicles)
                .HasForeignKey(v => v.GuestInvitationId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            entity.HasIndex(e => e.PlateNumber);
            entity.HasIndex(e => e.GuestInvitationId);
        });

        // Configure GuestInvitation
        builder.Entity<GuestInvitation>(entity =>
        {
            entity.HasKey(gi => gi.GuestInvitationId);

            entity.Property(e => e.InvitationNo)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.HostName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.HostEmail)
                .HasMaxLength(200);

            entity.Property(e => e.HostType)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(i => i.Guest)
                .WithMany(g => g.Invitations)
                .HasForeignKey(i => i.GuestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Host navigation property is optional (no foreign key constraint)
            // HostId can reference ApplicationUser but is not required
            entity.HasOne(i => i.Host)
                .WithMany()
                .HasForeignKey(i => i.HostId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasOne(i => i.Enterprise)
                .WithMany(e => e.Invitations)
                .HasForeignKey(i => i.EnterpriseId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.GuestId);
            entity.HasIndex(e => e.HostId);
            entity.HasIndex(e => e.EnterpriseId);
            entity.HasIndex(e => e.EnterpriseUserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpectedArrival);
            entity.HasIndex(e => e.InvitationNo)
                .IsUnique();
        });

        // Configure Approval
        builder.Entity<Approval>(entity =>
        {
            entity.HasKey(a => a.ApprovalId);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ApprovedAt)
                .IsRequired();

            entity.HasOne(a => a.GuestInvitation)
                .WithMany(i => i.Approvals)
                .HasForeignKey(a => a.GuestInvitationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.GuestInvitationId);
            entity.HasIndex(e => e.ApprovedBy);
        });

        // Configure NotificationEvent
        builder.Entity<NotificationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.TargetUserId, e.Processed, e.CreatedAt });
            entity.HasIndex(e => e.Processed);
            entity.HasIndex(e => e.ReferenceId);
        });

        // Configure Notification
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.DeliveryStatus)
                .IsRequired()
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt });
            entity.HasIndex(e => new { e.UserId, e.DeliveryStatus });
            entity.HasIndex(e => e.ReferenceId);
        });

        // Configure UserPresence
        builder.Entity<UserPresence>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.Property(e => e.LastHeartbeat)
                .IsRequired();

            entity.Property(e => e.ConnectionId)
                .HasMaxLength(100);

            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Identity tables
        builder.Entity<IdentityRole<int>>(entity =>
        {
            entity.ToTable("AspNetRoles");
        });

        builder.Entity<IdentityUserRole<int>>(entity =>
        {
            entity.ToTable("AspNetUserRoles");
        });

        builder.Entity<IdentityUserClaim<int>>(entity =>
        {
            entity.ToTable("AspNetUserClaims");
        });

        builder.Entity<IdentityUserLogin<int>>(entity =>
        {
            entity.ToTable("AspNetUserLogins");
        });

        builder.Entity<IdentityUserToken<int>>(entity =>
        {
            entity.ToTable("AspNetUserTokens");
        });
    }
}

