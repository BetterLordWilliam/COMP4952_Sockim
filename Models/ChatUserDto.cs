using System;

namespace COMP4952_Sockim.Models;

public class ChatUserDto : IEquatable<ChatUserDto>
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    public bool Equals(ChatUserDto? other)
    {
        return Id == other!.Id;
    }
}
