using System;
using Microsoft.AspNetCore.SignalR;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services;

namespace COMP4952_Sockim.Hubs;

/// <summary>
/// Template for InvitationHub implementation
/// Handles invitation acceptance and rejection
/// </summary>
public class InvitationHubTemplate : Hub
{
    ILogger<InvitationHubTemplate> _logger;
    InvitationsService _invitationsService;
    ChatService _chatService;
    ChatUserService _chatUserService;

    public InvitationHubTemplate(
        ILogger<InvitationHubTemplate> logger,
        InvitationsService invitationsService,
        ChatService chatService,
        ChatUserService chatUserService)
    {
        _logger = logger;
        _invitationsService = invitationsService;
        _chatService = chatService;
        _chatUserService = chatUserService;
    }

    /// <summary>
    /// User accepts an invitation to a chat
    /// </summary>
    public async Task AcceptInvitation(int invitationSenderId, int chatId, int receiverId)
    {
        try
        {
            _logger.LogInformation($"User {receiverId} accepting invitation from {invitationSenderId} for chat {chatId}");

            // TODO: Implement:
            // 1. Update invitation in database (Accepted = true)
            // 2. Add receiver to chat
            // 3. Broadcast update to all clients
            
            /*
            ChatInvitation? invitation = _chatDbContext.Invitations
                .FirstOrDefault(i => i.SenderId == invitationSenderId && 
                                     i.ReceiverId == receiverId && 
                                     i.ChatId == chatId);
            
            if (invitation != null)
            {
                invitation.Accepted = true;
                await _chatDbContext.SaveChangesAsync();
                
                Chat? chat = _chatService.GetChatById(chatId);
                ChatUser? receiver = _chatUserService.GetUser(receiverId);
                
                if (chat != null && receiver != null)
                {
                    chat.ChatUsers.Add(receiver);
                    await _chatDbContext.SaveChangesAsync();
                    
                    // Notify all clients
                    await Clients.All.SendAsync("InvitationAccepted", new { ChatId = chatId, UserId = receiverId });
                }
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error accepting invitation: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to accept invitation", error = ex.Message });
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

    /// <summary>
    /// Get all pending invitations for a user
    /// </summary>
    public async Task GetPendingInvitations(int userId)
    {
        try
        {
            // TODO: Implement:
            // 1. Query pending invitations for user
            // 2. Convert to DTOs
            // 3. Send back to caller

            /*
            ChatUser? user = _chatUserService.GetUser(userId);
            if (user != null)
            {
                ChatInvitation[] invitations = _chatDbContext.Invitations
                    .Where(i => i.ReceiverId == userId && !i.Accepted)
                    .ToArray();
                
                ChatInvitationDto[] invitationDtos = invitations
                    .Select(i => _invitationsService.ConvertToDto(i))
                    .ToArray();
                
                await Clients.Caller.SendAsync("PendingInvitations", invitationDtos);
            }
            */
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting pending invitations: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get invitations", error = ex.Message });
        }
    }
}
