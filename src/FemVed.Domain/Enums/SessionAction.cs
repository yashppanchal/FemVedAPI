namespace FemVed.Domain.Enums;

/// <summary>The action that triggered a <see cref="Entities.ProgramSessionLog"/> entry.</summary>
public enum SessionAction
{
    /// <summary>Expert or admin started the program for the user.</summary>
    Started,

    /// <summary>Expert, admin, or user paused the program.</summary>
    Paused,

    /// <summary>Expert or admin resumed a paused program.</summary>
    Resumed,

    /// <summary>Expert, admin, or user ended the program.</summary>
    Ended
}
