using System;
using COMP4952_Sockim.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
    DbSet<Chat> Chats { get; set; }
    DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>()
            .HasKey(e => e.ChatId);
        modelBuilder.Entity<Chat>()
            .HasMany(e => e.Messages)
            .WithOne(e => e.Chat)
            .HasForeignKey(e => e.ChatId)
            .IsRequired();

        modelBuilder.Entity<ChatMessage>()
            .HasKey(e => e.ChatId);
    }
}
