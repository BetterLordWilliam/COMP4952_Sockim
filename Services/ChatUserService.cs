using System;
using COMP4952_Sockim.Data;
using COMP4952_Sockim.Models;
using COMP4952_Sockim.Services.Exceptions;
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
    /// Retrives a user by their ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="ChatUserException"></exception>
    public ChatUserDto GetUser(int id)
    {
        try
        {
            ChatUser? user = _chatDbContext.Users
                .Where(u => u.Id == id)
                .FirstOrDefault();

            if (user is null)
            {
                _logger.LogError($"user not found with id {id}");
                throw new ChatUserNotFoundException($"user does not exist with id {id}");
            }

            _logger.LogInformation($"found user: {user.Email}");

            return ConvertToDto(user);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation to get user failed, {ex.Message}");
            throw new ChatUserException("internal error, failed to get user");
        }
    }

    /// <summary>
    /// Retrives a user by an email string.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="ChatUserDoesNotExistException"></exception>
    /// <exception cref="ChatUserException"></exception>
    public ChatUserDto GetUserByEmail(string email)
    {
        try
        {
            ChatUser? user = _chatDbContext.Users
                .Where(u => u.Email == email)
                .AsNoTracking()
                .FirstOrDefault();

            if (user is null)
            {
                _logger.LogError($"user not found with email {email}");
                throw new ChatUserNotFoundException($"user does not exist with email {email}");
            }

            _logger.LogInformation($"found user: {user.Email}");

            return ConvertToDto(user);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation to get user with email \'{email}\' failed: {ex.Message}");
            throw new ChatUserException("internal error, failed to get user by email");
        }
    }

    /// <summary>
    /// Retrieves an array of users by email.
    /// </summary>
    /// <param name="emails"></param>
    /// <returns></returns>
    /// <exception cref="ChatUserException"></exception>
    public ChatUserDto[] GetUserByEmail(string[] emails)
    {
        try
        {
            ChatUserDto[] users = _chatDbContext.Users
                .Where(u => emails.Contains(u.Email))
                .AsNoTracking()
                .Select(cu => ConvertToDto(cu))
                .ToArray();

            _logger.LogInformation($"found users: {users.Count()}");

            return users;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"operation to get user with emails failed: {ex.Message}");
            throw new ChatUserException("internal error, failed to retrieve users by email");
        }
    }

    /// <summary>
    /// Converts a chat user entity into a data transfer object.
    /// </summary>
    /// <param name="chatUser"></param>
    /// <returns></returns>
    public ChatUserDto ConvertToDto(ChatUser chatUser)
    {
        return new ChatUserDto()
        {
            Id = chatUser.Id,
            Email = chatUser?.Email ?? string.Empty
        };
    }
}
