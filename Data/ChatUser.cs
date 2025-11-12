using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace COMP4952_Sockim.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ChatUser : IdentityUser<int>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public override int Id { get; set; }
    [JsonIgnore]
    public List<Chat> Chats { get; set; } = [];
    [JsonIgnore]
    public List<Chat> OwnedChats { get; set; } = [];
    [JsonIgnore]
    public List<ChatMessage> ChatMessages { get; set; } = [];
    [JsonIgnore]
    public List<ChatInvitation> SentInvitations { get; set; } = [];
    [JsonIgnore]
    public List<ChatInvitation> ReceivedInvitations { get; set; } = [];
}
