using System;
using Microsoft.AspNetCore.SignalR;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
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

#region:User / Chat

    public async Task AddChatUser(int id)
    {
        string userGroupName = $"user-{id}";
        await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
    }

    public async Task RemoveChatUser(ChatUserDto chatUserDto)
    {
        string userGroupName = $"user-{chatUserDto.Id}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroupName);
    }

    public async Task ConnectToChatGroup(int chatId)
    {
        string chatGroupName = $"chat-{chatId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, chatGroupName);
        _logger.LogInformation(chatGroupName);
    }

    public async Task DisconnectToChatGroup(int chatId)
    {
        string chatGroupName = $"chat-{chatId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatGroupName);
    }

#endregion
#region:Chat

    /// <summary>
    /// Creates a new chat and sends invitations to specified users.
    /// </summary>
    public async Task AddChat(ChatDto chatDto, List<ChatInvitationDto> invitations)
    {
        try
        {
            _logger.LogInformation($"Adding new chat: {chatDto.ChatName}");

            ChatDto? createdChat = await _chatService.CreateChat(chatDto);
            if (createdChat == null)
            {
                _logger.LogError("Failed to create chat");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to create chat" });
                return;
            }

            bool success = await _invitationService.AddInvitations(invitations.ToArray());

            if (success)
            {
                _logger.LogInformation($"Added {invitations.Count} invitations for chat {createdChat.Id}");
            }
            else
            {
                _logger.LogWarning($"Failed to add invitations {chatDto.ChatName}, {chatDto.Id}");
            }

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
                    }
                }

            }

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

    /// <summary>
    /// Invite a user to an existing chat by email.
    /// </summary>
    public async Task InviteUserToChat(int chatId, int senderId, string receiverEmail)
    {
        try
        {
            _logger.LogInformation($"Inviting user {receiverEmail} to chat {chatId} by user {senderId}");

            // Verify chat and sender
            Chat? chat = await _chatService.GetChatById(chatId);
            if (chat == null)
            {
                _logger.LogError($"Chat {chatId} not found");
                await Clients.Caller.SendAsync("Error", new { message = "Chat not found" });
                return;
            }

            ChatUser? sender = _chatUserService.GetUser(senderId);
            ChatUser? invitee = _chatUserService.GetUserByEmail(receiverEmail);

            if (invitee is not null && sender is not null)
            {
                var invitationDto = new ChatInvitationDto
                {
                    ChatName = chat.ChatName,
                    SenderId = senderId,
                    SenderEmail = sender.Email ?? string.Empty,
                    ReceiverId = invitee.Id,
                    ReceiverEmail = invitee.Email ?? string.Empty,
                    ChatId = chatId,
                    Accepted = false
                };

                bool added = await _invitationService.AddInvitation(invitationDto);
                if (added)
                {
                    // Notify the recipient via InvitationHub group
                    await Clients.Group($"user-{invitationDto.ReceiverId}")
                        .SendAsync("IncomingInvitation", invitationDto);

                    // Confirm to sender
                    await Clients.Caller.SendAsync("InvitationSent", new { email = receiverEmail, chatId });
                    _logger.LogInformation($"Invitation sent to {receiverEmail} for chat {chatId}");
                }
                else
                {
                    _logger.LogError($"Failed to create invitation for {receiverEmail}");
                    await Clients.Caller.SendAsync("Error", new { message = "Failed to send invitation" });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error inviting user to chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to invite user", error = ex.Message });
        }
    }

    /// <summary>
    /// Remove a user from a chat.
    /// Only the chat owner can remove members.
    /// </summary>
    public async Task RemoveUserFromChat(int chatId, int requesterId, int userIdToRemove)
    {
        try
        {
            _logger.LogInformation($"User {requesterId} requesting to remove user {userIdToRemove} from chat {chatId}");

            // Get the chat to verify requester is the owner
            Chat? chat = await _chatService.GetChatById(chatId);
            if (chat == null)
            {
                _logger.LogError($"Chat {chatId} not found");
                await Clients.Caller.SendAsync("Error", new { message = "Chat not found" });
                return;
            }

            ChatDto? chatDto = _chatService.ConvertToDto(chat);

            // Prevent removing the owner
            if (chat.ChatOwnerId == userIdToRemove)
            {
                _logger.LogWarning($"Cannot remove chat owner from chat");
                await Clients.Caller.SendAsync("Error", new { message = "Cannot remove chat owner" });
                return;
            }

            // Remove the user from chat
            bool removed = await _chatService.RemoveUserFromChat(chatId, userIdToRemove);
            if (removed)
            {
                ChatUser? chatUser          = _chatUserService.GetUser(userIdToRemove);
                ChatUserDto? chatUserDto    = _chatUserService.ConvertToDto(chatUser);

                // Notify the removed user
                await Clients.Group($"user-{userIdToRemove}")
                    .SendAsync("RemovedFromChat", chatDto);

                // Notify chat members
                await Clients.Group($"chat-{chatId}")
                    .SendAsync("MemberRemoved", chatDto, chatUserDto);

                _logger.LogInformation($"User {userIdToRemove} removed from chat {chatId}");
            }
            else
            {
                _logger.LogError($"Failed to remove user {userIdToRemove} from chat {chatId}");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to remove member" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing user from chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to remove member", error = ex.Message });
        }
    }

    public async Task GetChatMembers(int chatId)
    {
        try
        {
            _logger.LogInformation($"retrieving members for chat {chatId}");

            ChatUserDto[] users = await _chatService.GetChatMembers(chatId);

            await Clients.Caller.SendAsync("RetrievedUsers", users);

            _logger.LogInformation($"send {users.Length} messages to called for chat {chatId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"error getting members from the chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to load chat members", error = ex.Message });
        }
    }

#endregion
#region:Chat Invitations

    /// <summary>
    /// Send an invitation to a single user.
    /// </summary>
    public async Task SendInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            _logger.LogInformation($"Sending invitation from {invitationDto.SenderId} to {invitationDto.ReceiverId} for chat {invitationDto.ChatId}");

            bool added = await _invitationService.AddInvitation(invitationDto);
            if (added)
            {
                // Notify the recipient
                await Clients.Group($"user-{invitationDto.ReceiverId}").SendAsync("IncomingInvitation", invitationDto);
                _logger.LogInformation($"Invitation sent to user {invitationDto.ReceiverId}");
            }
            else
            {
                _logger.LogError($"Failed to send invitation to user {invitationDto.ReceiverId}");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to send invitation" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending invitation: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to send invitation", error = ex.Message });
        }
    }

    /// <summary>
    /// Send invitations to multiple users.
    /// </summary>
    public async Task SendInvitations(ChatInvitationDto[] invitationDtos)
    {
        try
        {
            _logger.LogInformation($"Sending {invitationDtos.Length} invitations");

            bool allAdded = await _invitationService.AddInvitations(invitationDtos);
            if (allAdded)
            {
                foreach (var invitation in invitationDtos)
                {
                    // Notify each recipient
                    await Clients.Group($"user-{invitation.ReceiverId}")
                        .SendAsync("IncomingInvitation", invitation);
                }

                _logger.LogInformation($"All {invitationDtos.Length} invitations sent successfully");
            }
            else
            {
                _logger.LogError("Failed to send all invitations");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to send some invitations" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending invitations: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to send invitations", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all pending invitations for a user.
    /// Called when user first connects or refreshes.
    /// </summary>
    public async Task RetrieveInvitations(int userId)
    {
        try
        {
            _logger.LogInformation($"Retrieving pending invitations for user {userId}");

            ChatInvitationDto[] pendingInvitations = await _invitationService.GetUserInvitations(userId);
            await Clients.Caller.SendAsync("RetrievedInvitations", pendingInvitations);

            _logger.LogInformation($"Sent {pendingInvitations.Length} pending invitations to user {userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving invitations: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to get invitations", error = ex.Message });
        }
    }

    /// <summary>
    /// User accepts an invitation to a chat.
    /// </summary>
    public async Task AcceptInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            _logger.LogInformation($"User {invitationDto.ReceiverId} accepting invitation from {invitationDto.SenderId} for chat {invitationDto.ChatId}");

            // 1. Add receiver to chat (CRUD)
            bool userAdded = await _invitationService.AddUserToChat(invitationDto.ChatId, invitationDto.ReceiverId);
            if (!userAdded)
            {
                _logger.LogError($"Failed to add user {invitationDto.ReceiverId} to chat {invitationDto.ChatId}");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to accept invitation" });
                return;
            }

            ChatUser? chatUser = _chatUserService.GetUser(invitationDto.ReceiverId);
            ChatUserDto? chatUserDto = _chatUserService.ConvertToDto(chatUser);

            // 2. Delete the invitation (CRUD)
            bool invitationDeleted = await _invitationService.DeleteInvitation(
                invitationDto.SenderId,
                invitationDto.ReceiverId,
                invitationDto.ChatId);

            if (!invitationDeleted)
            {
                _logger.LogWarning($"Failed to delete invitation for user {invitationDto.ReceiverId}");
            }

            // 3. Get updated chat with all users
            Chat? chat = await _chatService.GetChatById(invitationDto.ChatId);
            if (chat == null)
            {
                _logger.LogError($"Chat {invitationDto.ChatId} not found after accepting invitation");
                await Clients.Caller.SendAsync("Error", new { message = "Chat not found" });
                return;
            }

            ChatDto updatedChat = _chatService.ConvertToDto(chat);

            // 4. Notify the accepting user
            await Clients.Group($"user-{invitationDto.ReceiverId}")
                .SendAsync("InvitationAccepted", updatedChat);

            // 5. Notify chat members that new user joined
            _logger.LogInformation($"{invitationDto.ChatId}, SENDING CHAT GROUP THAT NEW USER IS MEMBER OF THE GROUP");
            await Clients.Group($"chat-{invitationDto.ChatId}")
                .SendAsync("MemberJoined", chatUserDto);

            _logger.LogInformation($"User {invitationDto.ReceiverId} successfully accepted invitation for chat {invitationDto.ChatId}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error accepting invitation: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to accept invitation", error = ex.Message });
        }
    }

    /// <summary>
    /// User rejects an invitation to a chat.
    /// </summary>
    public async Task RejectInvitation(int senderId, int receiverId, int chatId)
    {
        try
        {
            _logger.LogInformation($"User {receiverId} rejecting invitation from {senderId} for chat {chatId}");

            // Delete the invitation (CRUD)
            bool deleted = await _invitationService.DeleteInvitation(senderId, receiverId, chatId);

            if (deleted)
            {
                // Notify the user
                await Clients.Group($"user-{senderId}").SendAsync("InvitationRejected", new { ChatId = chatId });
                _logger.LogInformation($"Invitation rejected successfully for user {receiverId}");
            }
            else
            {
                _logger.LogWarning($"Failed to delete invitation for rejection: {senderId}, {receiverId}, {chatId}");
                await Clients.Caller.SendAsync("Error", new { message = "Failed to reject invitation" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error rejecting invitation: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new { message = "Failed to reject invitation", error = ex.Message });
        }
    }

#endregion
}
