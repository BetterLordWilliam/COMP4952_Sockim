using System;

namespace COMP4952_Sockim.Services.Exceptions;

public class ChatException : Exception
{
    public ChatException(string message) : base(message) {}
}

public class ChatOwnerNotFound : Exception
{
    public ChatOwnerNotFound(string message) : base(message){}
}
