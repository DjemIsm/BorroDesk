using BorroDesk.Api.Authorization;
using BorroDesk.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BorroDesk.Api.Data;

public class BorroDeskDbContext(DbContextOptions<BorroDeskDbContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.Property(user => user.UserName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.NormalizedUserName)
                .HasMaxLength(100);

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(user => user.NormalizedEmail)
                .HasMaxLength(256);

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(500);

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(user => user.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");
        });

        modelBuilder.Entity<IdentityRole<int>>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasData(
                new IdentityRole<int>
                {
                    Id = 1,
                    Name = ApplicationRoles.User,
                    NormalizedName = ApplicationRoles.User.ToUpperInvariant(),
                    ConcurrencyStamp = "96ee0cc1-cc3b-4a9c-9de8-f67a361f1e79"
                },
                new IdentityRole<int>
                {
                    Id = 2,
                    Name = ApplicationRoles.Support,
                    NormalizedName = ApplicationRoles.Support.ToUpperInvariant(),
                    ConcurrencyStamp = "723052f5-2c2b-4465-ac7e-4340b1d72110"
                },
                new IdentityRole<int>
                {
                    Id = 3,
                    Name = ApplicationRoles.Admin,
                    NormalizedName = ApplicationRoles.Admin.ToUpperInvariant(),
                    ConcurrencyStamp = "c797372b-ac98-430f-b21f-1a7f3ca85862"
                });
        });

        modelBuilder.Entity<IdentityRoleClaim<int>>(entity => entity.ToTable("RoleClaims"));
        modelBuilder.Entity<IdentityUserClaim<int>>(entity => entity.ToTable("UserClaims"));
        modelBuilder.Entity<IdentityUserLogin<int>>(entity => entity.ToTable("UserLogins"));
        modelBuilder.Entity<IdentityUserRole<int>>(entity => entity.ToTable("UserRoles"));
        modelBuilder.Entity<IdentityUserToken<int>>(entity => entity.ToTable("UserTokens"));

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Tickets");

            entity.HasKey(ticket => ticket.Id);

            entity.Property(ticket => ticket.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(ticket => ticket.Description)
                .IsRequired();

            entity.Property(ticket => ticket.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(ticket => ticket.Priority)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(ticket => ticket.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(ticket => ticket.CreatedByUser)
                .WithMany(user => user.CreatedTickets)
                .HasForeignKey(ticket => ticket.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.AssignedToUser)
                .WithMany(user => user.AssignedTickets)
                .HasForeignKey(ticket => ticket.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.ToTable("TicketComments");

            entity.HasKey(comment => comment.Id);

            entity.Property(comment => comment.Comment)
                .IsRequired();

            entity.Property(comment => comment.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(comment => comment.Ticket)
                .WithMany(ticket => ticket.Comments)
                .HasForeignKey(comment => comment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comment => comment.User)
                .WithMany(user => user.TicketComments)
                .HasForeignKey(comment => comment.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketAttachment>(entity =>
        {
            entity.ToTable("TicketAttachments");

            entity.HasKey(attachment => attachment.Id);

            entity.Property(attachment => attachment.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(attachment => attachment.StoredFileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(attachment => attachment.FilePath)
                .HasMaxLength(1024)
                .IsRequired();

            entity.Property(attachment => attachment.ContentType)
                .HasMaxLength(127);

            entity.Property(attachment => attachment.UploadedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(attachment => attachment.Ticket)
                .WithMany(ticket => ticket.Attachments)
                .HasForeignKey(attachment => attachment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(attachment => attachment.UploadedByUser)
                .WithMany(user => user.TicketAttachments)
                .HasForeignKey(attachment => attachment.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
