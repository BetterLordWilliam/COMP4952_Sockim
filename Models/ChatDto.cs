using System;

namespace COMP4952_Sockim.Models;

public class ChatDto : IEquatable<ChatDto>
{
    public int Id { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public int ChatOwnerId { get; set; }
    public string ChatOwnerEmail { get; set; } = string.Empty;
    public List<int> ChatMemberIds { get; set; } = [];
    public List<string> InvitedEmails { get; set; } = [];
    public bool Unread { get; set; }
    public ChatMessageDto? MostRecent { get; set; }

    public bool Equals(ChatDto? other)
    {
        if (other is null)
            return false;

        return Id == other.Id;
    }

    public override string ToString()
    {
        return $"{{ Id: {Id}, "
            + $"ChatName: {ChatName},"
            + $"ChatOwnerId: {ChatOwnerId}, "
            + $"ChatOwnerEmail: {ChatOwnerEmail}, "
            + $"ChatMemberIds: {string.Join(',', ChatMemberIds)}, "
            + $"InvitedEmails: {string.Join(',', InvitedEmails)}, "
            + $"Unread: {Unread}, "
            + $"MostRecent: {MostRecent} }}";
    }
}
