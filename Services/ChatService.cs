using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services.Exceptions;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class ChatService
{
    private readonly ILogger<ChatService> _logger;
    private readonly ChatDbContext _chatDbContext;
    private readonly ChatUserService _chatUserService;

    public ChatService(ILogger<ChatService> logger, ChatDbContext chatDbContext, ChatUserService chatUserService)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
        _chatUserService = chatUserService;
    }

    /// <summary>
    /// Creates a new chat entity from a ChatDto, returns dto representing new chat when complete.
    /// </summary>
    /// <param name="chatDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatOwnerNotFound"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto> CreateChat(ChatDto chatDto)
    {
        try
        {
            ChatUser? owner = await _chatDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == chatDto.ChatOwnerId);

            if (owner is null)
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
            throw new ChatException();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"database update concurrency error while creating chat: {ex.Message}");
            throw new ChatException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"database update error while creating chat: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"database operation failed while creating chat {ex.Message}");
            throw new ChatException();
        }
    }

    public async Task DeleteChat(ChatDto chatDto)
    {

    }

    /// <summary>
    /// Gets a chat by it's id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto> GetChatById(int id)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatOwner)
                .Include(c => c.ChatUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat is null)
            {
                _logger.LogError($"no such chat, {id}");
                throw new ChatNotFoundException();
            }

            return ConvertToDto(chat);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled while getting chat: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation failed while getting chat: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Updates the properties of a chat (ie. renaming).
    /// </summary>
    /// <param name="chatDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto> UpdateChat(ChatDto chatDto)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Where(c => c.Id == chatDto.Id)
                .FirstOrDefaultAsync();

            if (chat is null)
            {
                _logger.LogError($"no such chat {chatDto.Id}");
                throw new ChatNotFoundException();
            }

            chat.ChatName = chatDto.ChatName;
            chat.ChatOwnerId = chatDto.ChatOwnerId;

            await _chatDbContext.SaveChangesAsync();

            return ConvertToDto(chat);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled while updating chat: {ex.Message}");
            throw new ChatException();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"operation concurrency error while updating chat: {ex.Message}");
            throw new ChatException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"operation update errror while updating chat: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation failed while getting chat: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Promotes a user to owner of the chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="newOwnerId"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatUserNotFoundException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto> PromoteToOwner(int chatId, int newOwnerId)
    {
        try 
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .Include(c => c.ChatOwner)
                .FirstOrDefaultAsync(c => c.Id == chatId);
            
            if (chat is null)
            {
                _logger.LogError($"Chat {chatId} not found");
                throw new ChatNotFoundException();
            }

            if (!chat.ChatUsers.Any(u => u.Id == newOwnerId))
            {
                _logger.LogError($"User {newOwnerId} is not a member of chat {chatId}");
                throw new ChatUserNotFoundException($"User {newOwnerId} is not a member of this chat");
            }

            chat.ChatOwnerId = newOwnerId;
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {newOwnerId} promoted to owner of chat {chatId}");
            
            return ConvertToDto(chat);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled while promoting: {ex.Message}");
            throw new ChatException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Database error while promoting: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Gets all the chats for a user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ChatException"></exception>
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
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"retriving chats for user operation cancelled: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation failed while getting chats for user {userId}: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Gets all users of a chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatUserDto[]> GetChatMembers(int chatId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat is null)
            {
                _logger.LogWarning($"Chat {chatId} not found when retrieving members");
                throw new ChatNotFoundException();
            }

            return chat.ChatUsers
                .Select(u => new ChatUserDto
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty
                })
                .ToArray();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"retriving chat members operation cancelled: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation failed while getting member for chat {chatId}: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Adds a user to a chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatUserNotFoundException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task AddUserToChat(int chatId, int userId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogError($"Chat with ID {chatId} not found");
                throw new ChatNotFoundException();
            }

            ChatUser? user = await _chatDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"User with ID {userId} not found");
                throw new ChatUserNotFoundException();
            }

            if (chat.ChatUsers.Any(cu => cu.Id == userId))
            {
                _logger.LogWarning($"User {userId} is already in chat {chatId}");
                throw new ChatException("cannot add user to the chat, they are already a member");
            }

            chat.ChatUsers.Add(user);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {userId} added to chat {chatId}");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"adding user {userId} to chat {chatId} failed, concurrency error: {ex.Message}");
            throw new ChatException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"adding user {userId} to chat {chatId} failed: {ex.Message}");
            throw new ChatException();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled while adding {userId} to chat {chatId}: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation failed while adding {userId} to chat {chatId}: {ex.Message}");
            throw new ChatException();
        }
    }

    /// <summary>
    /// Removes a user from a chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ChatNotFoundException"></exception>
    /// <exception cref="ChatUserNotFoundException"></exception>
    /// <exception cref="ChatOwnerCannotBeRemovedException"></exception>
    /// <exception cref="ChatException"></exception>
    public async Task<ChatDto?> RemoveUserFromChat(int chatId, int userId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogError($"Chat {chatId} not found");
                throw new ChatNotFoundException();
            }

            ChatUser? user = chat.ChatUsers.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                _logger.LogWarning($"User {userId} not found in chat {chatId}");
                throw new ChatUserNotFoundException($"user with id {userId} not found for chat {chatId}");
            }


            chat.ChatUsers.Remove(user);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {userId} removed from chat {chatId}");

            if (chat.ChatOwnerId == userId)
            {
                _logger.LogWarning("... Selecting a new owner ...");
                ChatUser? newOwner = chat.ChatUsers.FirstOrDefault();

                if (newOwner is null || chat.ChatUsers.Count() == 0)
                {
                    _logger.LogInformation("... No user to select, removing the chat ...");
                    _chatDbContext.Chats.Remove(chat);
                    await _chatDbContext.SaveChangesAsync();

                    return null;
                }
                else
                {
                    chat.ChatOwnerId = newOwner.Id;
                    chat.ChatOwner = newOwner;
                    newOwner.OwnedChats.Add(chat);    
                    await _chatDbContext.SaveChangesAsync();

                    return ConvertToDto(chat);
                }
            }
            else
            {
                return null;
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled while removing user from chat: {ex.Message}");
            throw new ChatException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"error removing user from chat: {ex.Message}");
            throw new ChatException();
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
