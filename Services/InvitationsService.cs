using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class InvitationsService
{
    private readonly ILogger<InvitationsService> _logger;
    private readonly ChatDbContext _chatDbContext;

    public InvitationsService(ILogger<InvitationsService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// Adds a user to a chat.
    /// Pure CRUD operation - adds ChatUser relationship without any orchestration.
    /// </summary>
    public async Task<bool> AddUserToChat(int chatId, int userId)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                _logger.LogError($"Chat with ID {chatId} not found");
                return false;
            }

            ChatUser? user = await _chatDbContext.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"User with ID {userId} not found");
                return false;
            }

            // Check if user is already in chat
            if (chat.ChatUsers.Any(cu => cu.Id == userId))
            {
                _logger.LogWarning($"User {userId} is already in chat {chatId}");
                return false;
            }

            chat.ChatUsers.Add(user);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"User {userId} added to chat {chatId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding user to chat: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Deletes an invitation by composite key.
    /// Pure CRUD operation.
    /// </summary>
    public async Task<bool> DeleteInvitation(int senderId, int receiverId, int chatId)
    {
        try
        {
            int deletedCount = await _chatDbContext.Invitations
                .Where(i => i.ChatId == chatId
                    && i.SenderId == senderId
                    && i.ReceiverId == receiverId)
                .ExecuteDeleteAsync();

            if (deletedCount > 0)
            {
                _logger.LogInformation($"Deleted invitation from {senderId} to {receiverId} for chat {chatId}");
                return true;
            }

            _logger.LogWarning($"No invitation found to delete for IDs: {senderId}, {receiverId}, {chatId}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting invitation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds multiple invitations from DTOs.
    /// </summary>
    public async Task<bool> AddInvitations(ChatInvitationDto[] invitationDtos)
    {
        try
        {
            List<ChatInvitation> invitations = new();
            foreach (var dto in invitationDtos)
            {
                invitations.Add(new ChatInvitation
                {
                    SenderId = dto.SenderId,
                    ReceiverId = dto.ReceiverId,
                    ChatId = dto.ChatId,
                    Accepted = dto.Accepted
                });
            }

            await _chatDbContext.Invitations.AddRangeAsync(invitations);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"Added {invitations.Count} invitations successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not add multiple invitations: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a single invitation from DTO.
    /// </summary>
    public async Task<bool> AddInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            ChatInvitation invitation = new()
            {
                SenderId = invitationDto.SenderId,
                ReceiverId = invitationDto.ReceiverId,
                ChatId = invitationDto.ChatId,
                Accepted = invitationDto.Accepted
            };

            await _chatDbContext.Invitations.AddAsync(invitation);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"Added invitation successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not add invitation: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets pending invitations for a specific user (non-tracking).
    /// </summary>
    public async Task<ChatInvitationDto[]> GetUserInvitations(int userId)
    {
        try
        {
            ChatInvitation[] invitations = await _chatDbContext.Invitations
                .Include(i => i.Chat)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Where(i => i.ReceiverId == userId && !i.Accepted)
                .AsNoTracking()
                .ToArrayAsync();

            return invitations.Select(i => new ChatInvitationDto
            {
                ChatId = i.ChatId,
                ChatName = i.Chat.ChatName,
                SenderId = i.SenderId,
                ReceiverId = i.ReceiverId,
                ReceiverEmail = i.Receiver.Email ?? string.Empty,
                SenderEmail = i.Sender.Email ?? string.Empty,
                Accepted = i.Accepted
            }).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not get invitations: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets an invitation by composite key.
    /// </summary>
    public async Task<ChatInvitation?> GetInvitation(int senderId, int receiverId, int chatId)
    {
        try
        {
            return await _chatDbContext.Invitations
                .Include(i => i.Chat)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .FirstOrDefaultAsync(i => i.ChatId == chatId
                    && i.SenderId == senderId
                    && i.ReceiverId == receiverId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving invitation: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a ChatInvitation entity to ChatInvitationDto.
    /// </summary>
    public ChatInvitationDto ConvertToDto(ChatInvitation invitation)
    {
        return new ChatInvitationDto
        {
            SenderId = invitation.SenderId,
            SenderEmail = invitation.Sender?.Email ?? string.Empty,
            ReceiverId = invitation.ReceiverId,
            ReceiverEmail = invitation.Receiver?.Email ?? string.Empty,
            ChatId = invitation.ChatId,
            Accepted = invitation.Accepted
        };
    }
}
