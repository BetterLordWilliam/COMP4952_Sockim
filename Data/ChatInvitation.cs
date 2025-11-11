using System;

namespace COMP4952_Sockim.Data;

public class ChatInvitation
{
    public int SenderId { get; set; }
    public ChatUser Sender { get; set; } = null!;
    public int ReceiverId { get; set; }
    public ChatUser Receiver { get; set; } = null!;
    public bool Accepted { get; set; }
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
}
