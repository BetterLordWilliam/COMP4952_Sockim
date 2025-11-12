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

    public ChatHub(ILogger<ChatHub> logger, ChatUserService chatUserService, ChatService chatService)
    {
        _logger = logger;
        _chatService = chatService;
        _chatUserService = chatUserService;
    }

    public Task AddChat(ChatDto chat, List<ChatInvidationDto> invitations)
    {
        _logger.LogInformation("Adding new chat");

        ChatUser owner = _chatUserService.GetUser()

        Chat newChat = new()
        {
            ChatName = chat.ChatName,
            ChatOwnerId = chat.ChatOwnerId,
        };

        _chatDbContext.Chats.AddAsync(newChat);
    }
}
