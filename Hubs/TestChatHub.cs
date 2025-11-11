using System;
using Microsoft.AspNetCore.SignalR;

namespace COMP4952_Sockim.Hubs;

public class TestChatHub : Hub
{
    public Task SendMessage(string user, string message)
    {
        return Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
