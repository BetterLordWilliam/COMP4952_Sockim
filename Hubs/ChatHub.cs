using System;
using Microsoft.AspNetCore.SignalR;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using MySqlX.XDevAPI;
using COMP4952_Sockim.Services;

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
                }

                await _invitationService.AddInvitations(invitations.ToArray());
                _logger.LogInformation($"Added {invitations.Count} invitations for chat {createdChat.Id}");
            }

            // Broadcast the new chat to all clients
            await Clients.All.SendAsync("ChatCreated", createdChat);
            _logger.LogInformation($"Chat '{createdChat.ChatName}' created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to create chat", error = ex.Message });
        }
    }

    public async Task<ChatDto[]> RetrieveChatsForUser(int userId)
    {
        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retriveing user chats {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get user chats" });
        }
    }
}
