using System;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services.Exceptions;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
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
    /// Adds a chat message.
    /// </summary>
    /// <param name="messageDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatMessageException"></exception>
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

            return ConvertToDto(message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"database concurrency error while adding message. {ex.Message}");
            throw new ChatMessageException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"database error while adding message: {ex.Message}");
            throw new ChatMessageException();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled error while adding message: {ex.Message}");
            throw new ChatMessageException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"error while adding message: {ex.Message}");
            throw new ChatMessageException();
        }
    }

    /// <summary>
    /// Updates a chat message.
    /// </summary>
    /// <param name="messageDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatMessageNotFoundException"></exception>
    /// <exception cref="ChatMessageException"></exception>
    public async Task<ChatMessageDto> UpdateChatMessage(ChatMessageDto messageDto)
    {
        try
        {
            ChatMessage? chatMessage = await _chatDbContext.Messages.Where(c => c.Id == messageDto.Id).FirstOrDefaultAsync();

            if (chatMessage is null)
            {
                _logger.LogError($"no such message {messageDto.Id}, in chat {messageDto.ChatId}");
                throw new ChatMessageNotFoundException("No such message");
            } 

            chatMessage.MessageContent = messageDto.MessageContent;
            await _chatDbContext.SaveChangesAsync();
            return messageDto;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"database concurrency error while updating message. {ex.Message}");
            throw new ChatMessageException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"database error while updating message: {ex.Message}");
            throw new ChatMessageException();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation cancelled error while updating message: {ex.Message}");
            throw new ChatMessageException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"error while updating message: {ex.Message}");
            throw new ChatMessageException();
        }
    }

    /// <summary>
    /// Retrieves messages for a chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    /// <exception cref="ChatMessageException"></exception>
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

            return messages.Select(m => ConvertToDto(m)).ToArray();
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled while retrieving messages for chat {chatId}: {ex.Message}");
            throw new ChatMessageException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"Error retrieving messages for chat {chatId}: {ex.Message}");
            throw new ChatMessageException();
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
