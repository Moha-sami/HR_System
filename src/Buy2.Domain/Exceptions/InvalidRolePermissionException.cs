namespace Buy2.Domain.Exceptions;

/// <summary>
/// Domain exception thrown when a role permission definition or assignment violates domain validation rules.
/// </summary>
public class InvalidRolePermissionException : Exception
{
    public InvalidRolePermissionException(string message)
        : base(message)
    {
    }

    public InvalidRolePermissionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
