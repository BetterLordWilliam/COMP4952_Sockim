using System;

namespace COMP4952_Sockim.Services.Exceptions;

public class ChatUserException : Exception
{
    public ChatUserException(string message = "") : base(message)
    {
    }
}

public class ChatUserNotFoundException : Exception
{
    public ChatUserNotFoundException(string message = "") : base(message)
    {
    }
}
