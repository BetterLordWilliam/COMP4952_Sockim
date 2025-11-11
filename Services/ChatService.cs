using System;
using COMP4952_Sockim.Data;

namespace COMP4952_Sockim.Services;

public class ChatService
{
    private ChatDbContext _chatDbContext;

    public ChatService(ChatDbContext chatDbContext)
    {
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// Adds a new chat.
    /// </summary>
    /// <param name="newChat"></param>
    public async void AddChat(Chat newChat)
    {
        var res = await _chatDbContext.Chats.AddAsync(newChat);
    }

    /// <summary>
    /// Gets all chats or the chats for a specific user.
    /// </summary>
    /// <param name="id"></param>
    public void GetChats(int? id)
    {

    }

    public void GetChats(ChatUser? chatUser)
    {

    }
}
