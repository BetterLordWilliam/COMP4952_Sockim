using System;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services;
using Microsoft.AspNetCore.SignalR;

namespace COMP4952_Sockim.Hubs;

public class MessageHub : Hub
{
    ILogger<MessageHubTemplate> _logger;
    MessagesService _messagesService;
    ChatService _chatService;
    ChatUserService _chatUserService;

    public MessageHub(
        ILogger<MessageHubTemplate> logger,
        MessagesService messagesService,
        ChatService chatService,
        ChatUserService chatUserService)
    {
        _logger = logger;
        _messagesService = messagesService;
        _chatService = chatService;
        _chatUserService = chatUserService;
    }

    /// <summary>
    /// User joins a chat room (should be called on connection)
    /// </summary>
    public async Task JoinChat(int chatId, int userId)
    {
        try
        {
            _logger.LogInformation($"User {userId} joining chat {chatId}");

            // Add user to a group for this chat
            string groupName = $"chat-{chatId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Notify others in the chat
            await Clients.Group(groupName).SendAsync("UserJoined", new { ChatId = chatId, UserId = userId });

            _logger.LogInformation($"User {userId} added to chat group {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error joining chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to join chat", error = ex.Message });
        }
    }

    /// <summary>
    /// User leaves a chat room (should be called on disconnect)
    /// </summary>
    public async Task LeaveChat(int chatId, int userId)
    {
        try
        {
            _logger.LogInformation($"User {userId} leaving chat {chatId}");

            string groupName = $"chat-{chatId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            // Notify others in the chat
            await Clients.Group(groupName).SendAsync("UserLeft", new { ChatId = chatId, UserId = userId });

            _logger.LogInformation($"User {userId} removed from chat group {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error leaving chat: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a message to a specific chat
    /// Message should already be saved to database before calling this
    /// </summary>
    public async Task SendMessage(int chatId, ChatMessageDto messageDto)
    {
        try
        {
            _logger.LogInformation($"Message sent to chat {chatId} by user {messageDto.ChatUserId}");

            // Add the message to the database
            await _messagesService.AddChatMessage(messageDto);

            // Broadcast to all users in the chat
            string groupName = $"chat-{chatId}";
            await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

            _logger.LogInformation($"Message broadcasted to chat group {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to send message", error = ex.Message });
        }
    }

    /// <summary>
    /// Get chat history (previous messages)
    /// Should be called when user first loads a chat
    /// </summary>
    public async Task GetChatHistory(int chatId)
    {
        try
        {
            _logger.LogInformation($"Retrieving chat history for chat {chatId}");

            ChatMessageDto[] messages = _messagesService.GetChatMessages(chatId);

            // Send history only to the caller
            await Clients.Caller.SendAsync("ChatHistory", messages);

            _logger.LogInformation($"Sent {messages.Length} messages to caller for chat {chatId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting chat history: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get chat history", error = ex.Message });
        }
    }

    /// <summary>
    /// Optional: Broadcasting when user is typing
    /// </summary>
    public async Task UserTyping(int chatId, int userId, string senderEmail)
    {
        try
        {
            string groupName = $"chat-{chatId}";
            
            // Broadcast to all EXCEPT sender
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserTyping", new 
            { 
                ChatId = chatId, 
                UserId = userId, 
                SenderEmail = senderEmail 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting typing: {ex.Message}");
        }
    }

    /// <summary>
    /// Optional: Broadcasting when user stops typing
    /// </summary>
    public async Task UserStoppedTyping(int chatId, int userId)
    {
        try
        {
            string groupName = $"chat-{chatId}";
            
            await Clients.GroupExcept(groupName, Context.ConnectionId).SendAsync("UserStoppedTyping", new 
            { 
                ChatId = chatId, 
                UserId = userId 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error broadcasting stopped typing: {ex.Message}");
        }
    }
}
