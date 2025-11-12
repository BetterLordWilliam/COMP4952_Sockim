using System;
using COMP4952_Sockim.Data;
using Microsoft.EntityFrameworkCore;

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
    /// Retrives a user by their id.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ChatUser? GetUser(int? id = null)
    {
        try
        {
            ChatUser user = (_chatDbContext.Users
                .Where(u => u.Id == id)
                .FirstOrDefault())!;

            return user;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation($"operation to get user failed: {ex.Message}");

            return null;
        }
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
                .AsNoTracking()
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

    public ChatUser[]? GetUserByEmail(string[] emails)
    {
        try
        {
            var users = _chatDbContext.Users
                .Where(u => emails.Contains(u.Email))
                .AsNoTracking()
                .ToArray();

            _logger.LogInformation($"found users: {users.Count()}");

            return users;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"operation to get user with emails failed: {ex.Message}");

            return null;
        }
    }
}
