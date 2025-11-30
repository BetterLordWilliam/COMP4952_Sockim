using System;
using Microsoft.AspNetCore.SignalR;

namespace COMP4952_Sockim.Hubs;

public class NotificationHub : Hub
{

    private ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

#region:stop / start

    public async Task ConnectUser()
    {

    }

    public async Task DisconnectUser()
    {
    }

#endregion

#region:chat message

#endregion

}
