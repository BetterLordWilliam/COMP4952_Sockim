using System;

namespace COMP4952_Sockim.Services.Exceptions;

public class ChatInvitationException : Exception
{
    public ChatInvitationException(string message = "") : base(message) {}
}


