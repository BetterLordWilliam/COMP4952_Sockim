using System;
using System.Linq.Expressions;
using COMP4952_Sockim.Data;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class MessagesService
{
    private ILogger<MessagesService> _logger;
    private ChatDbContext _chatDbContext;

    public MessagesService(ILogger<MessagesService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// Adds a new chat message to the database.
    /// </summary>
    /// <param name="chat"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task AddChatMessage(Chat chat, ChatMessage message)
    {
        try
        {
            chat.Messages.Add(message);
            await _chatDbContext.SaveChangesAsync();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"error while adding message {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the chat messages for a specific chat.
    /// </summary>
    /// <param name="chat"></param>
    /// <returns></returns>
    public ChatMessage[] GetChatMessages(Chat chat)
    {
        try
        {
            ChatMessage[] messages = _chatDbContext.Messages
                .Where(m => m.Chat == chat)
                .OrderBy(m => m.MessageDateTime)
                .AsNoTracking()
                .ToArray();

            return messages;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"failed getting messages for chat: {ex.Message}");

            return Array.Empty<ChatMessage>();
        }
    }
}
