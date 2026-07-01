using Microsoft.AspNetCore.Authorization;

namespace VSky.API.Authorization;

/// <summary>Authorization requirement satisfied when the caller may access a given module.</summary>
public class ModuleRequirement : IAuthorizationRequirement
{
    public string Module { get; }
    public ModuleRequirement(string module) => Module = module;
}
