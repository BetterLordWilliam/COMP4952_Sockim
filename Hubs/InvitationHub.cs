using System;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services;
using Microsoft.AspNetCore.SignalR;

namespace COMP4952_Sockim.Hubs;

public class InvitationHub : Hub
{
    ILogger<InvitationHub> _logger;
    InvitationsService _invitationsService;

    public InvitationHub(
        ILogger<InvitationHub> logger,
        InvitationsService invitationsService)
    {
        _logger = logger;
        _invitationsService = invitationsService;
    }

    public async Task AddInvitationUser(int userId)
    {
        string userGroup = $"user-{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);
    }

    public async Task RemoveInvitationUser(int userId)
    {
        string userGroup = $"user-{userId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroup);
    }

    /// <summary>
    /// User accepts an invitation to a chat
    /// </summary>
    public async Task AcceptInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            _logger.LogInformation($"User {invitationDto.ReceiverId} accepting invitation from {invitationDto.SenderId} for chat {invitationDto.ChatId}");

            // TODO: Implement:
            // 1. Update invitation in database (Accepted = true)
            // 2. Add receiver to chat
            // 3. Broadcast update to all clients

            invitationDto.Accepted = true;
            ChatDto? newChat = await _invitationsService.AcceptInvitation(invitationDto);

            if (newChat is not null)
            {
                _logger.LogInformation($"invitation accepted");
                await Clients.Group($"user-{invitationDto.ReceiverId}").SendAsync("InvitationAccepted", newChat);
            }
            else
            {
                _logger.LogError("error accepting invitation");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to accept invitation" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error accepting invitation: {ex.Message}");
        }
    }

    /// <summary>
    /// User rejects an invitation to a chat
    /// </summary>
    public async Task RejectInvitation(int invitationSenderId, int chatId, int receiverId)
    {
        try
        {
            _logger.LogInformation($"User {receiverId} rejecting invitation from {invitationSenderId} for chat {chatId}");

            // TODO: Implement:
            // 1. Delete invitation from database
            // 2. Notify relevant clients

            /*
            ChatInvitation? invitation = _chatDbContext.Invitations
                .FirstOrDefault(i => i.SenderId == invitationSenderId && 
                                     i.ReceiverId == receiverId && 
                                     i.ChatId == chatId);
            
            if (invitation != null)
            {
                _chatDbContext.Invitations.Remove(invitation);
                await _chatDbContext.SaveChangesAsync();
                
                // Notify relevant clients
                await Clients.All.SendAsync("InvitationRejected", new { ChatId = chatId, UserId = receiverId });
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rejecting invitation: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to reject invitation", error = ex.Message });
        }
    }

    public async Task SendInvitation(ChatInvitationDto chatInvitationDto)
    {
        try
        {
            await _invitationsService.AddInvitation(chatInvitationDto);
            await Clients.User($"{chatInvitationDto.ReceiverId}").SendAsync("IncomingInvitation", chatInvitationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError($"error sending invitation {ex.Message}");
        }
    }

    public async Task SendInvitations(ChatInvitationDto[] chatInvitationDtos)
    {
        try
        {
            foreach(ChatInvitationDto chatInvitationDto in chatInvitationDtos)
            {
                await _invitationsService.AddInvitation(chatInvitationDto);
                await Clients.User($"{chatInvitationDto.ReceiverId}").SendAsync("IncomingInvitation", chatInvitationDto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"error sending invitations {ex.Message}");
        }
    }

    /// <summary>
    /// Get all pending invitations for a user
    /// </summary>
    public async Task RetrieveInvitations(int userId)
    {
        try
        {
            // TODO: Implement:
            // 1. Query pending invitations for user
            // 2. Convert to DTOs
            // 3. Send back to caller

            ChatInvitationDto[] pendingInvitations = await _invitationsService.GetUserInvitation(userId);
            await Clients.Caller.SendAsync("RetrievedInvitations", pendingInvitations);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting pending invitations: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get invitations", error = ex.Message });
        }
    }
}
