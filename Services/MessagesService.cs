using System;
using COMP4952_Sockim.Data;

namespace COMP4952_Sockim.Services;

public class MessagesService
{
    private ChatDbContext _chatDbContext;

    public MessagesService(ChatDbContext chatDbContext)
    {
        _chatDbContext = chatDbContext;
    }
}
