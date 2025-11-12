using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;

namespace COMP4952_Sockim.Services;

public class InvitationsService
{
    ILogger<InvitationsService> _logger;
    ChatDbContext _chatDbContext;

    public InvitationsService(ILogger<InvitationsService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    public async Task<ChatDto?> AcceptInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            Chat chat = (await _chatDbContext.Chats
                .Include(c => c.ChatOwner)
                .Include(c => c.ChatUsers)
                .Where(c => c.Id == invitationDto.ChatId)
                .FirstOrDefaultAsync())!;
            ChatUser invitee = (await _chatDbContext.Users
                .Where(u => u.Id == invitationDto.ReceiverId)
                .FirstOrDefaultAsync())!;

            _logger.LogInformation($"user {invitee.Id}");

            chat.ChatUsers.Add(invitee);

            await _chatDbContext.SaveChangesAsync();

            await _chatDbContext.Invitations
                .Where(i => i.ChatId == invitationDto.ChatId
                            && i.SenderId == invitationDto.SenderId
                            && i.ReceiverId == invitationDto.ReceiverId)
                .ExecuteDeleteAsync();

            await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"user {invitationDto.ReceiverId} added to chat {invitationDto.ChatId}");

            return new ChatDto()
            {
                Id = chat.Id,
                ChatName = chat.ChatName,
                ChatOwnerId = chat.ChatOwnerId,
                ChatOwnerEmail = chat.ChatOwner.Email ?? string.Empty,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"could not accept invitation {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Adds multiple invitations from DTOs.
    /// </summary>
    public async Task AddInvitations(ChatInvitationDto[] invitationDtos)
    {
        try
        {
            List<ChatInvitation> invitations = [];
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not add multiple invitations: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Adds a single invitation from DTO.
    /// </summary>
    public async Task AddInvitation(ChatInvitationDto invitationDto)
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"Could not add invitation: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get invitations for a specific user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<ChatInvitationDto[]> GetUserInvitation(int userId)
    {
        try
        {
            ChatInvitation[] invitations = _chatDbContext.Invitations
                .Include(i => i.Chat)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .Where(i => i.ReceiverId == userId && !i.Accepted)
                .AsNoTracking()
                .ToArray();

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
