using System;
using Microsoft.AspNetCore.SignalR;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using MySqlX.XDevAPI;
using COMP4952_Sockim.Services;
using Microsoft.AspNetCore.Authorization;

namespace COMP4952_Sockim.Hubs;

public class ChatHub : Hub
{
    ILogger<ChatHub> _logger;
    ChatService _chatService;
    ChatUserService _chatUserService;
    InvitationsService _invitationService;

    public ChatHub(
        ILogger<ChatHub> logger,
        ChatUserService chatUserService,
        ChatService chatService,
        InvitationsService invitationService)
    {
        _logger = logger;
        _chatService = chatService;
        _chatUserService = chatUserService;
        _invitationService = invitationService;
    }

    public async Task AddNotificationUser(int id)
    {
        Console.WriteLine($"new user group {id}");
        string userGroupName = $"user-{id}";
        await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
    }

    public async Task RemoveNotificationUser(ChatUserDto chatUserDto)
    {
        string userGroupName = $"user-{chatUserDto.Id}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroupName);
    }

    /// <summary>
    /// Creates a new chat and sends invitations to specified users.
    /// Broadcasts the new chat to all connected clients.
    /// </summary>
    public async Task AddChat(ChatDto chatDto, List<ChatInvitationDto> invitations)
    {
        try
        {
            _logger.LogInformation($"Adding new chat: {chatDto.ChatName}");

            // Create the chat
            ChatDto createdChat = await _chatService.AddChatWithInvitations(chatDto);

            // Add the invitations
            if (invitations != null && invitations.Count > 0)
            {
                // Update invitation DTOs with the new chat ID
                foreach (var invitation in invitations)
                {
                    invitation.ChatId = createdChat.Id;
                    Console.WriteLine($"Receiver id: {invitation.ReceiverId}");

                    await _invitationService.AddInvitation(invitation);
                    await Clients.Group($"user-{invitation.ReceiverId}").SendAsync("IncomingInvitation", invitation);
                }

                _logger.LogInformation($"Added {invitations.Count} invitations for chat {createdChat.Id}");
            }

            _logger.LogInformation($"Chat '{createdChat.ChatName}' created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to create chat", error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve chatdtos for specific user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task RetrieveChats(int userId)
    {
        try
        {
            ChatDto[] userChats = _chatService.GetChatsForUser(userId);
            await Clients.Caller.SendAsync("RetrievedChats", userChats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retriveing user chats {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get user chats" });
        }
    }
}
