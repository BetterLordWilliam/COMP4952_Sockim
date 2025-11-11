using System;
using System.Security.Cryptography.Pkcs;
using COMP4952_Sockim.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace COMP4952_Sockim.Data;

public class ChatDbContext : IdentityDbContext<ChatUser, IdentityRole<int>, int>
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatMessage> Messages { get; set; }

    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Chat>(c =>
        {
            c.HasMany(e => e.ChatUsers)
            .WithMany(e => e.Chats)
            .UsingEntity("ChatParticipants");

            c.HasMany(e => e.Messages)
            .WithOne(e => e.Chat)
            .IsRequired()
            .HasForeignKey(e => e.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

            c.HasMany(e => e.Invitations)
            .WithOne(e => e.Chat)
            .IsRequired()
            .HasForeignKey(e => e.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

            c.HasOne(e => e.ChatOwner)
            .WithMany(e => e.OwnedChats)
            .IsRequired()
            .HasForeignKey(e => e.ChatOwnerId)
            .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ChatInvitation>()
            .HasKey(e => new { e.SenderId, e.ReceiverId, e.ChatId });

        builder.Entity<ChatUser>(cu =>
        {
            cu.HasMany(e => e.SentInvitations)
            .WithOne(e => e.Sender)
            .HasForeignKey(e => e.SenderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

            cu.HasMany(e => e.ReceivedInvitations)
            .WithOne(e => e.Receiver)
            .HasForeignKey(e => e.ReceiverId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChatMessage>()
            .HasOne(e => e.ChatUser)
            .WithMany(e => e.ChatMessages)
            .IsRequired()
            .HasForeignKey(e => e.ChatUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
