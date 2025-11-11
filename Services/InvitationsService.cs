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
            var addInvitationRes = await _chatDbContext.Invitations.AddAsync(invitation);
            var updateRes = await _chatDbContext.SaveChangesAsync();

            _logger.LogInformation($"curiosity {updateRes}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation($"could not add invitation: {ex.Message}");
        }
    }
}
