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
    /// Creates a new chat with the specified owner.
    /// Adds the owner to ChatUsers automatically.
    /// Pure CRUD - does not handle invitations.
    /// </summary>
    public async Task<ChatDto?> CreateChat(ChatDto chatDto)
    {
        try
        {
            // Get the owner
            ChatUser? owner = await _chatDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == chatDto.ChatOwnerId);

            if (owner == null)
            {
                _logger.LogError($"Owner with ID {chatDto.ChatOwnerId} not found");
                return null;
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
                InvitedEmails = new()
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Database error while creating chat: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while creating chat: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a chat by its ID (non-tracking).
    /// </summary>
    public async Task<Chat?> GetChatById(int id)
    {
        try
        {
            return await _chatDbContext.Chats
                .Include(c => c.ChatOwner)
                .Include(c => c.ChatUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled while getting chat: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets all chats for a specific user (non-tracking).
    /// </summary>
    public async Task<ChatDto[]> GetChatsForUser(int userId)
    {
        try
        {
            Chat[] chats = await _chatDbContext.Chats
                .Include(c => c.ChatOwner)
                .Include(c => c.Invitations)
                .Where(c => c.ChatUsers.Any(cu => cu.Id == userId))
                .AsNoTracking()
                .ToArrayAsync();

            return chats.Select(c => new ChatDto()
            {
                Id = c.Id,
                ChatName = c.ChatName,
                ChatOwnerId = c.ChatOwnerId,
                ChatOwnerEmail = c.ChatOwner.Email ?? string.Empty
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
