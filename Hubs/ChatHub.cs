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
    /// Orchestrates: Chat creation → Invitation creation → Broadcasting
    /// </summary>
    public async Task AddChat(ChatDto chatDto, List<ChatInvitationDto> invitations)
    {
        try
        {
            _logger.LogInformation($"Adding new chat: {chatDto.ChatName}");

            // 1. Create the chat (pure CRUD)
            ChatDto? createdChat = await _chatService.CreateChat(chatDto);
            if (createdChat == null)
            {
                _logger.LogError("Failed to create chat");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to create chat" });
                return;
            }

            // 2. Add invitations (pure CRUD)
            if (invitations != null && invitations.Count > 0)
            {
                foreach (var invitation in invitations)
                {
                    invitation.ChatId = createdChat.Id;
                    invitation.SenderId = chatDto.ChatOwnerId;

                    bool added = await _invitationService.AddInvitation(invitation);
                    if (added)
                    {
                        // 3. Notify recipient about invitation
                        await Clients.Group($"user-{invitation.ReceiverId}")
                            .SendAsync("IncomingInvitation", invitation);
                        _logger.LogInformation($"Invitation sent to user {invitation.ReceiverId}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to add invitation for user {invitation.ReceiverId}");
                    }
                }

                _logger.LogInformation($"Added {invitations.Count} invitations for chat {createdChat.Id}");
            }

            // 4. Confirm creation to caller
            await Clients.Caller.SendAsync("ChatCreated", createdChat);
            _logger.LogInformation($"Chat '{createdChat.ChatName}' created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to create chat", error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve chats for a specific user.
    /// </summary>
    public async Task RetrieveChats(int userId)
    {
        try
        {
            ChatDto[] userChats = await _chatService.GetChatsForUser(userId);
            await Clients.Caller.SendAsync("RetrievedChats", userChats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving user chats: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get user chats" });
        }
    }
}
