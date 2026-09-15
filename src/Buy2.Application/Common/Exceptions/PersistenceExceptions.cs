namespace Buy2.Application.Common.Exceptions;

/// <summary>
/// Persistence-agnostic failure for constraint/duplicate-key violations.
/// Thrown by infrastructure instead of leaking EF Core's DbUpdateException.
/// </summary>
public sealed class DataIntegrityException : Exception
{
    public DataIntegrityException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Persistence-agnostic failure for optimistic-concurrency conflicts.
/// Thrown by infrastructure instead of leaking EF Core's DbUpdateConcurrencyException.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
