using System;

namespace COMP4952_Sockim.Models;

public class ChatDto
{
    public int Id { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public int ChatOwnerId { get; set; }
}
