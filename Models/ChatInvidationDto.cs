using System;

namespace COMP4952_Sockim.Models;

public class ChatInvitationDto
{
    public int SenderId { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public int ReceiverId { get; set; }
    public string ReceiverEmail { get; set; } = string.Empty;
    public int ChatId { get; set; }
    public bool Accepted { get; set; }
}
