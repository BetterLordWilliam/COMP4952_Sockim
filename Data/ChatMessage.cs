using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace COMP4952_Sockim.Data;

public class ChatMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime MessageDateTime { get; set; }
    public string MessageContent { get; set; } = string.Empty;
    public int ChatId { get; set; }
    [JsonIgnore]
    public Chat Chat { get; set; } = null!;
    public int ChatUserId { get; set; }
    [JsonIgnore]
    public ChatUser ChatUser { get; set; } = null!;

    public override string ToString()
    {
        return $"{{ messageId: {Id}, senderEmail: {ChatUser.Email}, messageDateTime: {MessageDateTime} }}";
    }
}
