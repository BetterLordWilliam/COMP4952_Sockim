using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;

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
