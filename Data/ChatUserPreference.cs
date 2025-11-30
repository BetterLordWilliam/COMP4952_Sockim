using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace COMP4952_Sockim.Data;

public class ChatUserPreference
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ChatId { get; set; }

    [Required]
    public int MemberId { get; set; }

    [Required]
    [MaxLength(7)]
    public string Color { get; set; } = "#6e8cfb14";

    public ChatUser User { get; set; } = null;
    public Chat Chat { get; set; } = null;
}