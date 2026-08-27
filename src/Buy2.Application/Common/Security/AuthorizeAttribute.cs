using System;

namespace Buy2.Application.Common.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
public class AuthorizeAttribute : Attribute
{
    public AuthorizeAttribute() { }

    public string Roles { get; set; } = string.Empty;
    public string Policies { get; set; } = string.Empty;
}
