using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class ChatService
{
    private ILogger<ChatService> _logger;
    private ChatDbContext _chatDbContext;

    public ChatService(ILogger<ChatService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// Creates a new chat with invitations sent to specified email addresses.
    /// Returns the created chat as a DTO.
    /// </summary>
    public async Task<ChatDto> AddChatWithInvitations(ChatDto chatDto)
    {
        try
        {
            // Get the owner
            ChatUser? owner = _chatDbContext.Users
                .Where(u => u.Id == chatDto.ChatOwnerId)
                .FirstOrDefault();

            if (owner == null)
            {
                _logger.LogError($"Owner with ID {chatDto.ChatOwnerId} not found");
                throw new InvalidOperationException("Chat owner not found");
            }

            // Create the chat entity
            Chat chat = new()
            {
                ChatName = chatDto.ChatName,
                ChatOwnerId = chatDto.ChatOwnerId,
                ChatOwner = owner
            };
            
            // Add the owner to the chat
            chat.ChatUsers.Add(owner);

            // Add the chat to database
            _chatDbContext.Chats.Add(chat);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"Chat '{chatDto.ChatName}' created with ID {chat.Id}");

            // Convert back to DTO and return
            return new ChatDto
            {
                Id = chat.Id,
                ChatName = chat.ChatName,
                ChatOwnerId = chat.ChatOwnerId,
                ChatOwnerEmail = owner.Email ?? string.Empty,
                InvitedEmails = chatDto.InvitedEmails
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Database error while creating chat: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while creating chat: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a chat by its Id.
    /// </summary>
    public Chat? GetChatById(int id)
    {
        try
        {
            var chat = _chatDbContext.Chats
                .Where(c => c.Id == id)
                .AsNoTracking()
                .FirstOrDefault();

            return chat;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled while getting chat: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all the chats for a specific user.
    /// </summary>
    public ChatDto[] GetChatsForUser(int userId)
    {
        try
        {
            var chats = _chatDbContext.Chats
                .Include(c => c.ChatOwner)
                .Include(c => c.Invitations)
                .Where(c => c.ChatUsers.Any(cu => cu.Id == userId))
                .AsNoTracking()
                .ToArray();

            Console.WriteLine(chats[0].Invitations[0].SenderId);

            return chats.Select(c => new ChatDto()
            {
                Id = c.Id,
                ChatName = c.ChatName,
                ChatOwnerId = c.ChatOwnerId,
                ChatOwnerEmail = c.ChatOwner.Email ?? string.Empty
                // InvitedEmails = c.Invitations.Select(i => i.Receiver.Email ?? string.Empty).ToList()
            }).ToArray();
        }
        catch (NullReferenceException ex)
        {
            _logger.LogError($"Null reference error: {ex.Message}");
            return Array.Empty<ChatDto>();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled: {ex.Message}");
            return Array.Empty<ChatDto>();
        }
    }

    /// <summary>
    /// Converts a Chat entity to ChatDto.
    /// </summary>
    public ChatDto ConvertToDto(Chat chat)
    {
        return new ChatDto
        {
            Id = chat.Id,
            ChatName = chat.ChatName,
            ChatOwnerId = chat.ChatOwnerId,
            ChatOwnerEmail = chat.ChatOwner?.Email ?? string.Empty,
            InvitedEmails = chat.ChatUsers
                .Where(u => u.Id != chat.ChatOwnerId)
                .Select(u => u.Email ?? string.Empty)
                .ToList()
        };
    }
}
