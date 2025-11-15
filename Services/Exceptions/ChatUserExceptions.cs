using System;

namespace COMP4952_Sockim.Services.Exceptions;

public class ChatUserException : Exception
{
    public ChatUserException(string message) : base(message)
    {
    }
}

public class ChatUserDoesNotExistException : Exception
{
    public ChatUserDoesNotExistException(string message) : base(message)
    {
    }
}
