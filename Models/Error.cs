using System;

namespace COMP4952_Sockim.Models;

/// <summary>
/// Represents an application error. Returned by hubs if error conditions met.
/// </summary>
public class SockimError
{
    public required string Message { get; set; }
    public required Exception Exception { get; set; }
}
