using System;

namespace COMP4952_Sockim.Models;

public class ChatInvidationDto
{
    public int SenderId { get; set; }
    public int RecieverId { get; set; }
    public int ChatId { get; set; }
    public bool Accepted { get; set; }
}
