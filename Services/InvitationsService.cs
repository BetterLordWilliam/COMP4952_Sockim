using System;
using COMP4952_Sockim.Data;

namespace COMP4952_Sockim.Services;

public class InvitationsService
{
    ChatDbContext _chatDbContext;

    public InvitationsService(ChatDbContext chatDbContext)
    {
        _chatDbContext = chatDbContext;
    }


}
