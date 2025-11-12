using System;
using System.Text.Json.Serialization;

namespace COMP4952_Sockim.Data;

public class ChatInvitation
{
    public int SenderId { get; set; }
    [JsonIgnore]
    public ChatUser Sender { get; set; } = null!;
    public int ReceiverId { get; set; }
    [JsonIgnore]
    public ChatUser Receiver { get; set; } = null!;
    public bool Accepted { get; set; }
    public int ChatId { get; set; }
    [JsonIgnore]
    public Chat Chat { get; set; } = null!;
}
