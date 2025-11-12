using System;
using System.Linq.Expressions;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
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
    /// Adds a new chat message to the database from a DTO.
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

            _logger.LogInformation($"Message added with ID {message.Id}");

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
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Error while adding message: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Retrieves the chat messages for a specific chat as DTOs.
    /// </summary>
    public ChatMessageDto[] GetChatMessages(int chatId)
    {
        try
        {
            ChatMessage[] messages = _chatDbContext.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.MessageDateTime)
                .AsNoTracking()
                .ToArray();

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
            _logger.LogError($"Failed getting messages for chat: {ex.Message}");
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
