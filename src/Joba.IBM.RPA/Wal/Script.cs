namespace Joba.IBM.RPA
{
    public record class Script(Guid Id, string Name);
    public record class ScriptVersion(Guid Id, Guid ScriptId, int Version, Version ProductVersion, string Content);
}
