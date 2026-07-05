namespace Callsign.AlphaSmoke;

public static class PackTestSupport
{
    public static Callsign.Extensions.CallsignCommandRegistry CreateRegistry()
    {
        var root = Path.Combine(Path.GetTempPath(), "Callsign.AlphaSmoke", "packs", Guid.NewGuid().ToString("N"));
        return new Callsign.Extensions.CallsignCommandRegistry(root);
    }
}
