using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services.Exceptions;
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
    /// Deletes a chat invitation.
    /// </summary>
    /// <param name="senderId"></param>
    /// <param name="receiverId"></param>
    /// <param name="chatId"></param>
    /// <returns></returns>
    /// <exception cref="ChatInvitationException"></exception>
    public async Task DeleteInvitation(int senderId, int receiverId, int chatId)
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
            }

            _logger.LogWarning($"No invitation found to delete for IDs: {senderId}, {receiverId}, {chatId}");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"error deleting invitation: {ex.Message}");
            throw new ChatInvitationException("could not delete invitation");
        }
    }

    /// <summary>
    /// Adds multiple chat invitations
    /// </summary>
    /// <param name="invitationDtos"></param>
    /// <exception cref="ChatInvitationException"></exception>
    /// <returns></returns>
    public async Task AddInvitations(ChatInvitationDto[] invitationDtos)
    {
        foreach (ChatInvitationDto invitation in invitationDtos)
        {
            await AddInvitation(invitation);
        }

        _logger.LogInformation($"Added invitations successfully");
    }

    /// <summary>
    /// Adds chat invitation
    /// </summary>
    /// <param name="invitationDto"></param>
    /// <returns></returns>
    /// <exception cref="ChatInvitationException"></exception>
    public async Task AddInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            Chat? chat = await _chatDbContext.Chats
                .Include(c => c.ChatUsers)
                .Where(c => c.Id == invitationDto.ChatId)
                .FirstOrDefaultAsync();

            if (chat is null)
            {
                _logger.LogError($"chat with the id {invitationDto.ChatId} does not exist");
                throw new ChatInvitationException();
            }

            if (chat.ChatUsers.Where(c => c.Id == invitationDto.ReceiverId).FirstOrDefault() is not null)
            {
                _logger.LogError($"user with id {invitationDto.ReceiverId} is already a member of chat {chat.Id}");
                throw new ChatInvitationUserInvitedException();
            }

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
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"add invitation operation cancelled: {ex.Message}");
            throw new ChatInvitationException();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError($"could not add multiple invitations, concurrency exception: {ex.Message}");
            throw new ChatInvitationException();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError($"could not add invitation for {invitationDto.ReceiverId} to chat {invitationDto.ChatId}: {ex.Message}");
            throw new ChatInvitationException();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"could not add invitation: {ex.Message}");
            throw new ChatInvitationException();
        }
    }

    /// <summary>
    /// Retrieves a users invitations
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ChatInvitationException"></exception>
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
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"could not get invitations for user: {ex.Message}");
            throw new ChatInvitationException("could not get user invitations");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"could not get invitations: {ex.Message}");
            throw new ChatInvitationException("could not get user invitations");
        }
    }

    /// <summary>
    /// Gets a specific invitation.
    /// </summary>
    /// <param name="senderId"></param>
    /// <param name="receiverId"></param>
    /// <param name="chatId"></param>
    /// <returns></returns>
    /// <exception cref="ChatInvitationException"></exception>
    public async Task<ChatInvitationDto> GetInvitation(int senderId, int receiverId, int chatId)
    {
        try
        {
            ChatInvitation? chatInvitation = await _chatDbContext.Invitations
                .Include(i => i.Chat)
                .Include(i => i.Sender)
                .Include(i => i.Receiver)
                .FirstOrDefaultAsync(i => i.ChatId == chatId
                    && i.SenderId == senderId
                    && i.ReceiverId == receiverId);

            if (chatInvitation is null)
                throw new ChatInvitationException("no such invitation exists");

            return ConvertToDto(chatInvitation);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"could not retrieving invitation: {ex.Message}");
            throw new ChatInvitationException("could not get invitation for user");
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"could not retrieving invitation: {ex.Message}");
            throw new ChatInvitationException("could not get invitation for user");
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
