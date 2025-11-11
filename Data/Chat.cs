using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Data;

public class Chat
{
    public int Id { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public int ChatOwnerId { get; set; }
    public ChatUser ChatOwner { get; set; } = null!;
    public List<ChatUser> ChatUsers { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];
    public List<ChatInvitation> Invitations { get; set; } = [];

    public override string ToString()
    {
        string emails = "";
        for (int i = 0; i < ChatUsers.Count; i++)
        {
            if (i > 0)
            {
                emails += $", {ChatUsers[i].Email}";
            }
            else
            {
                emails += ChatUsers[i].Email;
            }
        }
        return $"{{ ChatName: {ChatName}, OwnerEmail: {ChatOwner.Email}, UserEmails: [{emails}] }}";
    }
}
