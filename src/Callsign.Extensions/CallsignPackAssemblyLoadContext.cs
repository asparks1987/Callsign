using System.Reflection;
using System.Runtime.Loader;

namespace Callsign.Extensions;

internal sealed class CallsignPackAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _assemblyPath;

    public CallsignPackAssemblyLoadContext(string assemblyPath)
        : base(isCollectible: true)
    {
        _assemblyPath = Path.GetFullPath(assemblyPath);
        _resolver = new AssemblyDependencyResolver(assemblyPath);
    }

    public Assembly LoadMainAssembly()
    {
        using var stream = File.OpenRead(_assemblyPath);
        return LoadFromStream(stream);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return resolvedPath == null ? null : LoadFromAssemblyPath(resolvedPath);
    }
}
