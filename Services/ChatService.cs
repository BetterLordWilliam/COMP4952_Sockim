using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services.Exceptions;
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
    /// Creates a new chat entity from a ChatDto, returns dto representing new chat when complete.
    /// </summary>
    /// <param name="chatDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatOwnerNotFound"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto?> CreateChat(ChatDto chatDto)
    {
        try
        {
            ChatUser? owner = await _chatDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == chatDto.ChatOwnerId);
            if (owner == null)
            {
                _logger.LogError($"Owner with ID {chatDto.ChatOwnerId} not found");
                throw new ChatOwnerNotFound($"owner with id {chatDto.ChatOwnerId}");
            }

            Chat chat = new()
            {
                ChatName = chatDto.ChatName,
                ChatOwnerId = chatDto.ChatOwnerId,
                ChatOwner = owner
            };
            chat.ChatUsers.Add(owner);
            _chatDbContext.Chats.Add(chat);

            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"Chat '{chatDto.ChatName}' created with ID {chat.Id}");

            return ConvertToDto(chat);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"database operation cancelled while creating chat: {ex.Message}");
            throw new ChatException("could not create chat");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"database update concurrency error while creating chat: {ex.Message}");
            throw new ChatException("internal error could not create chat");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"database update error while creating chat: {ex.Message}");
            throw new ChatException("internal error could not create chat");
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
    /// Will apply the differences in the incoming chatDto to the chat entity in the database.
    /// </summary>
    /// <param name="chatDto"></param>
    /// <returns></returns>
    public async Task<ChatDto?> UpdateChat(ChatDto chatDto)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Where(c => c.Id == chatDto.Id)
                .FirstOrDefaultAsync();

            if (chat is null)
                throw new Exception("no such chat exists.");

            chat.ChatName = chatDto.ChatName;
            chat.ChatOwnerId = chatDto.ChatOwnerId;

            await _chatDbContext.SaveChangesAsync();

            return ConvertToDto(chat);
        }
        catch (Exception ex)
        {
            _logger.LogError($"error while updating chat: {ex.Message}");
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

    /// <summary>
    /// Gets all chat members (non-tracking) including owner.
    /// Useful for displaying members and managing who can remove users.
    /// </summary>
    public async Task<ChatUserDto[]> GetChatMembers(int chatId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogWarning($"Chat {chatId} not found when retrieving members");
                return Array.Empty<ChatUserDto>();
            }

            return chat.ChatUsers
                .Select(u => new ChatUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty
                })
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving chat members for chat {chatId}: {ex.Message}");
            return Array.Empty<ChatUserDto>();
        }
    }

    /// <summary>
    /// Adds a user to a chat.
    /// Pure CRUD operation - adds ChatUser relationship without any orchestration.
    /// </summary>
    public async Task<bool> AddUserToChat(int chatId, int userId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogError($"Chat with ID {chatId} not found");
                return false;
            }

            ChatUser? user = await _chatDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"User with ID {userId} not found");
                return false;
            }

            // Check if user is already in chat
            if (chat.ChatUsers.Any(cu => cu.Id == userId))
            {
                _logger.LogWarning($"User {userId} is already in chat {chatId}");
                return false;
            }

            chat.ChatUsers.Add(user);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {userId} added to chat {chatId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding user to chat: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a user from a chat.
    /// Can only be called if the requester is the chat owner (business logic in hub).
    /// </summary>
    public async Task<bool> RemoveUserFromChat(int chatId, int userId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogError($"Chat {chatId} not found");
                return false;
            }

            ChatUser? user = chat.ChatUsers.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found in chat {chatId}");
                return false;
            }

            // Don't allow removing the owner
            if (chat.ChatOwnerId == userId)
            {
                _logger.LogWarning($"Cannot remove chat owner {userId} from chat {chatId}");
                return false;
            }

            chat.ChatUsers.Remove(user);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {userId} removed from chat {chatId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing user from chat: {ex.Message}");
            return false;
        }
    }
}
