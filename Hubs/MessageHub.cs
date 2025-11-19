using System;
using System.Text;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services;
using COMP4952_Sockim.Services.Exceptions;
using Microsoft.AspNetCore.SignalR;

namespace COMP4952_Sockim.Hubs;

public class MessageHub : Hub
{
    private readonly ILogger<MessageHub> _logger;
    private readonly MessagesService _messagesService;
    private readonly ChatService _chatService;
    private readonly ChatUserService _chatUserService;

    public MessageHub(
        ILogger<MessageHub> logger,
        MessagesService messagesService,
        ChatService chatService,
        ChatUserService chatUserService)
    {
        _logger = logger;
        _messagesService = messagesService;
        _chatService = chatService;
        _chatUserService = chatUserService;
    }

#region:User / Message

    /// <summary>
    /// User joins a chat room (should be called on connection)
    /// </summary>
    public async Task JoinChat(int chatId, int userId)
    {
        try
        {
            _logger.LogInformation($"User {userId} joining chat {chatId}");

            string groupName = $"chat-{chatId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).SendAsync("UserJoined", new { ChatId = chatId, UserId = userId });

            _logger.LogInformation($"User {userId} joined chat group {groupName}");
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
            _logger.LogInformation($"User {userId} disconnecting from chat group {chatId}");

            string groupName = $"chat-{chatId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            // Notify others in the chat
            await Clients.Group(groupName).SendAsync("UserLeft", new { ChatId = chatId, UserId = userId });

            _logger.LogInformation($"User {userId} disconnected from chat group {groupName}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error leaving chat: {ex.Message}");
        }
    }

#endregion
#region:Messaging

    public async Task SendMessage(int chatId, ChatMessageDto messageDto)
    {
        try
        {
            _logger.LogInformation($"Message sent to chat {chatId} by user {messageDto.ChatUserId}");

            ChatMessageDto newMessage = await _messagesService.AddChatMessage(messageDto);

            string groupName = $"chat-{chatId}";
            await Clients.Group(groupName).SendAsync("ReceiveMessage", newMessage);

            _logger.LogInformation($"Message broadcasted to chat group {groupName}");
        }
        catch (ChatMessageException ex)
        {
            string msg = "internal error, could not send message to chat";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task EditMessage(ChatMessageDto messageDto)
    {
        try
        {
            _logger.LogInformation($"Message {messageDto.Id} edited in chat {messageDto.ChatId}");

            await _messagesService.UpdateChatMessage(messageDto);
            await Clients.Group($"chat-{messageDto.ChatId}").SendAsync("MessageUpdated", messageDto);
        }
        catch (ChatMessageNotFoundException ex)
        {
            string msg = "cannot update a message that does not exist";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatMessageException ex)
        {
            string msg = "internal error, could not update message";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task GetChatHistory(int chatId)
    {
        try
        {
            _logger.LogInformation($"Retrieving chat history for chat {chatId}");

            ChatMessageDto[] messages = await _messagesService.GetChatMessages(chatId);

            await Clients.Caller.SendAsync("RetrievedMessages", messages);

            _logger.LogInformation($"Sent {messages.Length} messages to caller for chat {chatId}");
        }
        catch (ChatMessageException ex)
        {
            string msg = "internal error, could not get messages for chat.";
            _logger.LogError($"Error getting chat history: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task DeleteMessage(ChatMessageDto message)
    {
        try
        {
            _logger.LogInformation($"deleting message with id {message.Id}");

            await _messagesService.DeleteChatMessage(message.Id);

            _logger.LogInformation($"deleted message with id {message.Id}");

            await Clients.Caller.SendAsync("MessageDeletedSuccess", new SockimMessage()
            {
                Message = "message deleted successfully"
            });
            await Clients.Group($"chat-{message.ChatId}").SendAsync("MessageDeleted", message);
        }
        catch (ChatMessageException ex)
        {
            string msg = "chat message could not be deleted";
            _logger.LogError($"{msg}, {ex.Message}");
            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg
            });
        }
    }

    // public async Task 

    /// <summary>
    /// Broadcasting when user is typing.
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
    /// Broadcasting when user stops typing.
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

#endregion

}
