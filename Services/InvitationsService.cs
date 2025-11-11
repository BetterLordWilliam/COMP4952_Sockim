using System;
using System.Threading.Tasks;
using COMP4952_Sockim.Data;

namespace COMP4952_Sockim.Services;

public class InvitationsService
{
    ILogger<InvitationsService> _logger;
    ChatDbContext _chatDbContext;

    public InvitationsService(ILogger<InvitationsService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    public async Task AddInvitation(ChatInvitation invitation)
    {
        try
        {
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation($"could not add invitation: {ex.Message}");
        }
    }
}
