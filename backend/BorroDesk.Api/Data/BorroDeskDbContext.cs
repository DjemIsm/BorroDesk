using BorroDesk.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BorroDesk.Api.Data;

public class BorroDeskDbContext(DbContextOptions<BorroDeskDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.UserName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(user => user.UserName)
                .IsUnique();

            entity.Property(user => user.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

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
