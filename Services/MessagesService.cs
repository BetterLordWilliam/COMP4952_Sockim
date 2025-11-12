using System;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class MessagesService
{
    private readonly ILogger<MessagesService> _logger;
    private readonly ChatDbContext _chatDbContext;

    public MessagesService(ILogger<MessagesService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// Adds a new chat message to the database from a DTO.
    /// Sets MessageDateTime to current UTC time.
    /// </summary>
    public async Task<ChatMessageDto> AddChatMessage(ChatMessageDto messageDto)
    {
        try
        {
            ChatMessage message = new()
            {
                ChatId = messageDto.ChatId,
                ChatUserId = messageDto.ChatUserId,
                MessageContent = messageDto.MessageContent,
                MessageDateTime = DateTime.UtcNow
            };

            _chatDbContext.Messages.Add(message);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"Message {message.Id} added to chat {message.ChatId}");

            return new ChatMessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                ChatUserId = message.ChatUserId,
                SenderEmail = messageDto.SenderEmail,
                MessageDateTime = message.MessageDateTime,
                MessageContent = message.MessageContent
            };
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"Database error while adding message: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while adding message: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves chat messages for a specific chat (non-tracking).
    /// Returns messages ordered by datetime (ascending).
    /// </summary>
    public async Task<ChatMessageDto[]> GetChatMessages(int chatId)
    {
        try
        {
            ChatMessage[] messages = await _chatDbContext.Messages
                .Include(m => m.ChatUser)
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.MessageDateTime)
                .AsNoTracking()
                .ToArrayAsync();

            return messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                ChatUserId = m.ChatUserId,
                SenderEmail = m.ChatUser?.Email ?? string.Empty,
                MessageDateTime = m.MessageDateTime,
                MessageContent = m.MessageContent
            }).ToArray();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled while retrieving messages for chat {chatId}: {ex.Message}");
            return Array.Empty<ChatMessageDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving messages for chat {chatId}: {ex.Message}");
            return Array.Empty<ChatMessageDto>();
        }
    }

    /// <summary>
    /// Converts a ChatMessage entity to ChatMessageDto.
    /// </summary>
    public ChatMessageDto ConvertToDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            ChatId = message.ChatId,
            ChatUserId = message.ChatUserId,
            SenderEmail = message.ChatUser?.Email ?? string.Empty,
            MessageDateTime = message.MessageDateTime,
            MessageContent = message.MessageContent
        };
    }
}
