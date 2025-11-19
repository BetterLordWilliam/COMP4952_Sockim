using System;
using Microsoft.AspNetCore.SignalR;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services;
using COMP4952_Sockim.Services.Exceptions;

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

    /// <summary>
    /// Creates group for a connection -- representative of a user.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task AddChatUser(int id)
    {
        string userGroupName = $"user-{id}";
        await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
    }

    /// <summary>
    /// Removes connection from group for a chat user.
    /// </summary>
    /// <param name="chatUserDto"></param>
    /// <returns></returns>
    public async Task RemoveChatUser(ChatUserDto chatUserDto)
    {
        string userGroupName = $"user-{chatUserDto.Id}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroupName);
    }

    /// <summary>
    /// Connects to a group for a specific chat.
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public async Task ConnectToChatGroup(int chatId)
    {
        string chatGroupName = $"chat-{chatId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, chatGroupName);
        _logger.LogInformation(chatGroupName);
    }

    /// <summary>
    /// Remobes connection form a group for a specific.
    /// </summary>
    /// <param name="chatId"></param>
    /// <returns></returns>
    public async Task DisconnectToChatGroup(int chatId)
    {
        string chatGroupName = $"chat-{chatId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatGroupName);
    }

#endregion
#region:Chat

    public async Task AddChat(ChatDto chatDto)
    {
        try
        {
            _logger.LogInformation($"Adding new chat: {chatDto.ChatName}");

            ChatDto createdChat = await _chatService.CreateChat(chatDto);

            await Clients.Caller.SendAsync("ChatCreated", createdChat);
            _logger.LogInformation($"Chat '{createdChat.ChatName}' created successfully");
        }
        catch (ChatOwnerNotFound ex)
        {
            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = $"could not create chat for owner {chatDto.ChatOwnerEmail}, owner id {chatDto.ChatOwnerId}",
            });
        }
        catch (ChatException ex)
        {
            _logger.LogError($"Error adding chat: {ex.Message}");
            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = "Failed to create chat",
            });
        }
    }

    public async Task RenameChat(ChatDto chat)
    {
        try
        {
            ChatDto newChat = await _chatService.UpdateChat(chat);

            await Clients.Group($"chat-{chat.Id}").SendAsync("ChatUpdated", newChat);

            // Notify chat users that are not connected currently to the chat that it as updated
            ChatUserDto[] chatUsers = await _chatService.GetChatMembers(newChat.Id);
            foreach (ChatUserDto chatUser in chatUsers)
            {
                await Clients.Group($"user-{chatUser.Id}").SendAsync("ChatUpdated", newChat);
            }
        }
        catch (ChatNotFoundException ex)
        {
            string msg = "cannot rename a chat that does not exist";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatException ex)
        {
            string msg = "internal error, could not rename chat";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task RetrieveChats(int userId)
    {
        try
        {
            ChatDto[] userChats = await _chatService.GetChatsForUser(userId);
            await Clients.Caller.SendAsync("RetrievedChats", userChats);
        }
        catch (ChatException ex)
        {
            string msg = "internal error, could not retrieve chats for user";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task InviteUserToChat(int chatId, int senderId, string receiverEmail)
    {
        try
        {
            _logger.LogInformation($"Inviting user {receiverEmail} to chat {chatId} by user {senderId}");

            ChatDto chat = await _chatService.GetChatById(chatId);
            ChatUserDto sender = _chatUserService.GetUser(senderId);
            ChatUserDto receiver = _chatUserService.GetUserByEmail(receiverEmail);
            ChatInvitationDto invitationDto = new()
            {
                ChatName = chat.ChatName,
                SenderId = senderId,
                SenderEmail = sender.Email ?? string.Empty,
                ReceiverId = receiver.Id,
                ReceiverEmail = receiver.Email ?? string.Empty,
                ChatId = chatId,
                Accepted = false
            };

            await _invitationService.AddInvitation(invitationDto);

            await Clients.Group($"user-{invitationDto.ReceiverId}")
                .SendAsync("IncomingInvitation", invitationDto);
            await Clients.Caller
                .SendAsync("InvitationSent", new SockimMessage()
                {
                    Message = $"invitation sent to \"{invitationDto.ReceiverEmail}\""
                });

            _logger.LogInformation($"Invitation sent to {receiverEmail} for chat {chatId}");
        }
        catch (ChatNotFoundException ex)
        {
            string msg = "tried to invite user to a chat that does not exist";
            _logger.LogError(msg);

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatUserNotFoundException ex)
        {
            string msg = "tried to invite user that does not exist";
            _logger.LogError(msg);

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatInvitationUserInvitedException ex)
        {
            string msg = "tried to invite a user to a chat they are already a part of";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg
            });
        }
        catch (Exception ex) when (ex is ChatUserException || ex is ChatException || ex is ChatInvitationException)
        {
            string msg = "internal error, could not invite user";
            _logger.LogError($"{msg}, {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task InviteUsersToChat(ChatDto chatDto, ChatUser user, List<string> recieverEmails)
    {
        try
        {
        }
        catch (Exception ex)
        {
        }
    }

    public async Task RemoveUserFromChat(int chatId, int userIdToRemove)
    {
        try
        {
            _logger.LogInformation($"Requesting to remove user {userIdToRemove} from chat {chatId}");

            ChatDto chatDto = await _chatService.GetChatById(chatId);
            ChatDto? updatedChat = await _chatService.RemoveUserFromChat(chatId, userIdToRemove);
            ChatUserDto chatUserDto = _chatUserService.GetUser(userIdToRemove);

            await Clients.Group($"user-{userIdToRemove}")
                .SendAsync("RemovedFromChat", chatDto);

            await Clients.Group($"chat-{chatId}")
                .SendAsync("MemberRemoved", chatDto, chatUserDto);

            _logger.LogInformation($"User {userIdToRemove} removed from chat {chatId}");

            if (updatedChat is not null)
            {
                await Clients.Group($"chat-{chatId}")
                    .SendAsync("ChatUpdated", updatedChat);
            }
        }
        catch (ChatNotFoundException ex)
        {
            string msg = "you cannot remvoe a user from a chat that does not exist";
            _logger.LogError($"Chat {chatId} not found: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatUserNotFoundException ex)
        {
            string msg = "you cannot remove this user";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatOwnerCannotBeRemovedException ex)
        {
            string msg = "you cannot remove the chat owner";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (Exception ex) when (ex is ChatException || ex is ChatUserException)
        {
            string msg = "internal error, could not remove user from chat";
            _logger.LogError($"Error removing user from chat: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
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
        catch (ChatNotFoundException ex)
        {
            string msg = "cannot get chat members for a chat that does not exist";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatException ex)
        {
            string msg = "internal error, could not get users for chat";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

#endregion
#region:Chat Invitations

    public async Task SendInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            _logger.LogInformation($"Sending invitation from {invitationDto.SenderId} to {invitationDto.ReceiverId} for chat {invitationDto.ChatId}");

            await _invitationService.AddInvitation(invitationDto);

            await Clients.Group($"user-{invitationDto.ReceiverId}").SendAsync("IncomingInvitation", invitationDto);

            _logger.LogInformation($"Invitation sent to user {invitationDto.ReceiverId}");
            {
                await Clients.Caller.SendAsync("Error", new { message = "Failed to send invitation" });
            }
        }
        catch (ChatInvitationException ex)
        {
            string msg = $"Failed to send invitation to user {invitationDto.ReceiverId}";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task SendInvitations(ChatInvitationDto[] invitationDtos)
    {
        try
        {
            _logger.LogInformation($"Sending {invitationDtos.Length} invitations");

            await _invitationService.AddInvitations(invitationDtos);

            foreach (var invitation in invitationDtos)
            {
                // Notify each recipient
                await Clients.Group($"user-{invitation.ReceiverId}")
                    .SendAsync("IncomingInvitation", invitation);
            }

            _logger.LogInformation($"All {invitationDtos.Length} invitations sent successfully");
        }
        catch (ChatInvitationException ex)
        {
            string msg = "internal error adding chat invitation";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task RetrieveInvitations(int userId)
    {
        try
        {
            _logger.LogInformation($"Retrieving pending invitations for user {userId}");

            ChatInvitationDto[] pendingInvitations = await _invitationService.GetUserInvitations(userId);
            await Clients.Caller.SendAsync("RetrievedInvitations", pendingInvitations);

            _logger.LogInformation($"Sent {pendingInvitations.Length} pending invitations to user {userId}");
        }
        catch (ChatInvitationException ex)
        {
            string msg = "internal error, could not retrieve invitations";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task AcceptInvitation(ChatInvitationDto invitationDto)
    {
        try
        {
            _logger.LogInformation($"User {invitationDto.ReceiverId} accepting invitation from {invitationDto.SenderId} for chat {invitationDto.ChatId}");

            await _chatService.AddUserToChat(invitationDto.ChatId, invitationDto.ReceiverId);

            ChatUserDto chatUserDto = _chatUserService.GetUser(invitationDto.ReceiverId);

            await _invitationService.DeleteInvitation(
                invitationDto.SenderId,
                invitationDto.ReceiverId,
                invitationDto.ChatId);

            ChatDto updatedChat = await _chatService.GetChatById(invitationDto.ChatId);

            await Clients.Group($"user-{invitationDto.ReceiverId}")
                .SendAsync("InvitationAccepted", updatedChat);

            _logger.LogInformation($"{invitationDto.ChatId}, SENDING CHAT GROUP THAT NEW USER IS MEMBER OF THE GROUP");

            await Clients.Group($"chat-{invitationDto.ChatId}")
                .SendAsync("MemberJoined", chatUserDto);

            _logger.LogInformation($"User {invitationDto.ReceiverId} successfully accepted invitation for chat {invitationDto.ChatId}");
        }
        catch (ChatNotFoundException ex)
        {
            string msg = "cannot accept invitation to chat that does not exist";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (ChatUserNotFoundException ex)
        {
            string msg = "cannot accept invitation if you do not exist";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
        catch (Exception ex) when (ex is ChatException || ex is ChatUserException || ex is ChatInvitationException)
        {
            string msg = "internal error, could not accept invitation";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

    public async Task RejectInvitation(int senderId, int receiverId, int chatId)
    {
        try
        {
            _logger.LogInformation($"User {receiverId} rejecting invitation from {senderId} for chat {chatId}");

            await _invitationService.DeleteInvitation(senderId, receiverId, chatId);

            await Clients.Group($"user-{senderId}").SendAsync("InvitationRejected", new { ChatId = chatId });
            _logger.LogInformation($"Invitation rejected successfully for user {receiverId}");
        }
        catch (ChatInvitationException ex)
        {
            string msg = $"internal error could not reject invitation.";
            _logger.LogError($"{msg}: {ex.Message}");

            await Clients.Caller.SendAsync("Error", new SockimError()
            {
                Message = msg,
            });
        }
    }

#endregion
}
