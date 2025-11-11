using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace COMP4952_Sockim.Models;

public class Chat
{
    [Key]
    public int ChatId { get; set; }
    public string ChatName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public List<string> UserEmails { get; set; } = [];
    public List<ChatMessage> Messages { get; set; } = [];

    public override string ToString()
    {
        string emails = "";
        for (int i = 0; i < UserEmails.Count; i++)
        {
            if (i > 0)
            {
                emails += $", {UserEmails[i]}";
            }
            else
            {
                emails += UserEmails[i];
            }
        }
        return $"{{ ChatName: {ChatName}, OwnerEmail: {OwnerEmail}, UserEmails: [{emails}] }}";
    }
}
