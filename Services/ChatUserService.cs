using System;
using COMP4952_Sockim.Data;

namespace COMP4952_Sockim.Services;

public class ChatUserService
{
    private ILogger<ChatUserService> _logger;
    private ChatDbContext _chatDbContext;

    public ChatUserService(ILogger<ChatUserService> logger, ChatDbContext chatDbContext)
    {
        _logger = logger;
        _chatDbContext = chatDbContext;
    }

    /// <summary>
    /// retrieves a user given a specific email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public ChatUser? GetUserByEmail(string email)
    {
        try
        {
            var user = _chatDbContext.Users
                .Where(u => u.Email == email)
                .FirstOrDefault();

            _logger.LogInformation($"found user: {user}");

            return user;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation to get user with email \'{email}\' failed: {ex.Message}");

            return null;
        }
    }
}
