using System;

namespace COMP4952_Sockim.Models;

public class ChatMessageDto : IEquatable<ChatMessageDto>
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int ChatUserId { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime MessageDateTime { get; set; }
    public string MessageContent { get; set; } = string.Empty;

    public bool Equals(ChatMessageDto? other)
    {
        if (other is null)
            return false;

        return Id == other.Id;
    }
}
