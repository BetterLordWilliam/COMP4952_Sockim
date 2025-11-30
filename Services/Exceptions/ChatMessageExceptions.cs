using System;

namespace COMP4952_Sockim.Services.Exceptions;

public class ChatMessageException : Exception
{
    public ChatMessageException(string message = "") : base(message)
    {
    }

}

public class ChatMessageNotFoundException : Exception
{
    public ChatMessageNotFoundException(string message = "") : base(message)
    {
    }
}
