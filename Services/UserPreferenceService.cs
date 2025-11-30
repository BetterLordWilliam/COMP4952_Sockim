using COMP4952_Sockim.Data;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Services;

public class UserPreferenceService
{
    private readonly ChatDbContext _context;

    public UserPreferenceService(ChatDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<int, string>> GetChatUserPreferencesAsync(int userId, int chatId)
    {
        var preferences = await _context.UserPreferences
            .Where(p => p.UserId == userId && p.ChatId == chatId)
            .ToDictionaryAsync(p => p.MemberId, p => p.Color);

        return preferences;
    }

    public async Task SaveUserPreferencesAsync(int userId, int chatId, int memberId, string color)
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ChatId == chatId && p.MemberId == memberId);

        if (existing != null)
        {
            existing.Color = color;
        }
        else
        {
            _context.UserPreferences.Add(new ChatUserPreference
            {
                UserId = userId,
                ChatId = chatId,
                MemberId = memberId,
                Color = color
            });
        }

        await _context.SaveChangesAsync();
    }
}