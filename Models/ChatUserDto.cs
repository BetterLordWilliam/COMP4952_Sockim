using System;

namespace COMP4952_Sockim.Models;

public class ChatUserDto : IEquatable<ChatUserDto>
{
    public required int Id { get; set; }
    public required string Email { get; set; } = string.Empty;

    public bool Equals(ChatUserDto? other)
    {
        return Id == other!.Id;
    }
}
