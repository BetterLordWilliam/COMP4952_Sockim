using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace COMP4952_Sockim.Models;

public class ChatMessage
{
    [Key]
    public int ChatMessageId { get; set; }
    [ForeignKey(nameof(Chat.ChatId))]
    public int ChatId { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public DateTime MessageDateTime { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public Chat Chat { get; set; } = null!;


    public override string ToString()
    {
        return $"{{ messageId: {ChatMessageId}, senderEmail: {SenderEmail}, messageDateTime: {MessageDateTime} }}";
    }
}
