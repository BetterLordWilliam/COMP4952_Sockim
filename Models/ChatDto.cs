using System;

namespace COMP4952_Sockim.Models;

public class ChatDto : IEquatable<ChatDto>
{
    public int Id { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public int ChatOwnerId { get; set; }
    public string ChatOwnerEmail { get; set; } = string.Empty;
    public List<string> InvitedEmails { get; set; } = [];

    public bool Equals(ChatDto? other)
    {
        if (other is null)
            return false;

        return Id == other.Id;
    }
}
