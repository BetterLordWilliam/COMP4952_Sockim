using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
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
    /// Adds a new chat.
    /// </summary>
    /// <param name="newChat"></param>
    public async void AddChat(Chat newChat)
    {
        try
        {
            var addRes  = await _chatDbContext.Chats.AddAsync(newChat);
            var saveRes = await _chatDbContext.SaveChangesAsync();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Gets a chat by its Id.
    /// </summary>
    /// <param name="id"></param>
    public async Task<Chat[]> GetChats(int? id)
    {
        try
        {
            var chats = (id is null)
                ? _chatDbContext.Chats.ToArray()
                : _chatDbContext.Chats.Where(c => c.Id == id).ToArray();

            return chats;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex.Message);

            return Array.Empty<Chat>();
        }
    }

    /// <summary>
    /// Gets all the chats for a specific user.
    /// </summary>
    /// <param name="chatUser"></param>
    /// <returns></returns>
    public async Task<Chat[]> GetChatsForUser(ChatUser? chatUser)
    {
        try
        {
            var chats = _chatDbContext.Chats
                .Where(c => c.ChatUsers.Contains(chatUser!)).ToArray();

            return chats;
        }
        catch (NullReferenceException ex)
        {
            _logger.LogError($"no chats for null user: {ex.Message}");

            return Array.Empty<Chat>();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"db operation cancelled: {ex.Message}");

            return Array.Empty<Chat>();
        }
    }
}
