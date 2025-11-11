using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace COMP4952_Sockim.Data;

public class ChatMessage
{
    public int Id { get; set; }
    public DateTime MessageDateTime { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;
    public int ChatUserId { get; set; }
    public ChatUser ChatUser { get; set; } = null!;

    public override string ToString()
    {
        return $"{{ messageId: {Id}, senderEmail: {ChatUser.Email}, messageDateTime: {MessageDateTime} }}";
    }
}
