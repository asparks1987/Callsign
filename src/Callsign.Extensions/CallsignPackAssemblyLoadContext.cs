using System.Reflection;
using System.Runtime.Loader;

namespace Callsign.Extensions;

internal sealed class CallsignPackAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public CallsignPackAssemblyLoadContext(string assemblyPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return resolvedPath == null ? null : LoadFromAssemblyPath(resolvedPath);
    }
}
