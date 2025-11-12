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

    public async Task AddChat(ChatDto chat, List<ChatInvidationDto> invitations)
    {
        _logger.LogInformation("Adding new chat");

        ChatUser owner = _chatUserService.GetUser()!;
        Chat newChat = new()
        {
            ChatName = chat.ChatName,
            ChatOwnerId = chat.ChatOwnerId
        };
        newChat.ChatUsers.Add(owner);

        await _chatService.AddChat(newChat);

        List<ChatInvitation> newInvitations = [];
        foreach (ChatInvidationDto invitation in invitations)
        {
            ChatInvitation i = new()
            {
                ChatId = newChat.Id,
                SenderId = invitation.SenderId,
                ReceiverId = invitation.RecieverId
            };
            newInvitations.Add(i);
        }

        await _invitationService.Add
    }
}
