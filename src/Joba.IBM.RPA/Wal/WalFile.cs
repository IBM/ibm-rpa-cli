using Joba.IBM.RPA.Server;
using ProtoBuf;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Joba.IBM.RPA
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public sealed class WalFile
    {
        public const string Extension = ".wal";

        private WalFile(FileInfo file, WalFileProto proto)
        {
            Info = file;
            Name = new WalFileName(Info.Name);
            Content = new WalContent(proto.Content);
            Id = proto.Id;
            VersionId = proto.VersionId;
            Version = WalVersion.Create(proto.Version);
            if (proto.ProductVersion != null)
                ProductVersion = new Version(proto.ProductVersion);
        }

        internal WalFile(FileInfo file, ScriptVersion version)
        {
            Info = file;
            Name = new WalFileName(Info.Name);
            UpdateWith(version);
        }

        public FileInfo Info { get; }
        public WalFileName Name { get; }
        public bool IsFromServer => Id.HasValue;
        internal WalContent Content { get; private set; }
        internal Guid? Id { get; set; }
        internal Guid? VersionId { get; set; }
        public WalVersion? Version { get; set; }
        internal Version? ProductVersion { get; set; }
        internal WalVersion NextVersion => Version.HasValue ? new WalVersion(Version.Value.ToInt32() + 1) : new WalVersion(1);

        public async Task UpdateToLatestAsync(IScriptResource resource, CancellationToken cancellation)
        {
            if (!IsFromServer)
                throw new Exception($"The wal file '{Info.Name}' has not been downloaded from the server");

            var version = await resource.GetLatestVersionAsync(Id!.Value, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of {Info.Name}");

            if (version.Version < Version)
                throw new Exception($"The local '{Version}' version is greater than the latest server {version.Version}' version");
            if (version.Version != Version)
            {
                UpdateWith(version);
                Save();
            }
        }

        public async Task OverwriteToLatestAsync(IScriptResource resource, string fileName, CancellationToken cancellation)
        {
            var version = await resource.GetLatestVersionAsync(Path.GetFileNameWithoutExtension(fileName), cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of {fileName}");

            if (version.Version < Version)
                throw new Exception($"The local '{Version}' version is greater than the latest server '{version.Version}' version");

            if (version.Version != Version)
            {
                UpdateWith(version);
                Save();
            }
        }

        public void Overwrite(WalContent content)
        {
            Content = content;
            Save();
        }

        internal void Overwrite(ScriptVersion version)
        {
            UpdateWith(version);
            Save();
        }

        internal void Overwrite(Guid scriptId, Guid versionId, WalVersion version)
        {
            Id = scriptId;
            VersionId = versionId;
            Version = version;
            Save();
        }

        private void UpdateWith(ScriptVersion version)
        {
            Content = new WalContent(version.Content);
            Id = version.ScriptId;
            VersionId = version.Id;
            Version = version.Version;
            ProductVersion = version.ProductVersion;
        }

        internal PublishScript PrepareToPublish(string message, TimeSpan timeout, bool resetIds = false) =>
            new(resetIds ? null : Id, resetIds ? null : VersionId, Name.WithoutExtension, message, Content.ToString(), ProductVersion?.ToString(), false, Convert.ToInt32(timeout.TotalSeconds), Convert.ToInt32(timeout.TotalSeconds), Convert.ToInt32(timeout.TotalSeconds));

        internal void Save()
        {
            using var stream = new FileStream(Info.FullName, FileMode.Create);
            var proto = new WalFileProto
            {
                Content = Content.ToString(),
                Id = Id,
                ProductVersion = ProductVersion?.ToString(),
                Version = Version?.ToInt32(),
                VersionId = VersionId
            };
            Serializer.Serialize(stream, proto);
        }

        internal WalFile Clone() => CloneTo(Info);

        public WalFile CloneTo(FileInfo file)
        {
            var proto = new WalFileProto { Content = Content.ToString(), Id = Id, ProductVersion = ProductVersion?.ToString(), Version = Version?.ToInt32(), VersionId = VersionId };
            return new WalFile(file, proto);
        }

        public override string ToString() => Content.ToString();

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is WalFile wal)
                return wal.Info.FullName == Info.FullName;
            return false;
        }
        public override int GetHashCode() => Info.FullName.GetHashCode();
        public static bool operator ==(WalFile? left, WalFile? right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        public static bool operator !=(WalFile? left, WalFile? right) => !(left == right);

        public static WalFile Read(FileInfo file)
        {
            using var stream = File.OpenRead(file.FullName);
            var proto = Serializer.Deserialize<WalFileProto>(stream);
            return new WalFile(file, proto);
        }

        public static WalContent ReadAllText(FileInfo file)
        {
            var wal = Read(file);
            return wal.Content;
        }
        private string GetDebuggerDisplay() => $"{Name} v{Version ?? new WalVersion(0)} => v{NextVersion}";

        [ProtoContract]
        protected class WalFileProto
        {
            [ProtoMember(1)]
            public Guid? Id { get; set; }

            [ProtoMember(2)]
            public required string Content { get; set; }

            [ProtoMember(3)]
            public int? Version { get; set; }

            [ProtoMember(4)]
            public Guid? VersionId { get; set; }

            [ProtoMember(5)]
            public string? ProductVersion { get; set; }
        }
    }

    public struct WalContent
    {
        private readonly string content;

        public WalContent(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            this.content = content;
        }

        public static WalContent Build(IEnumerable<string> lines) =>
            new WalContent(string.Join(System.Environment.NewLine, lines));

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is WalContent wal)
                return wal.content == content;
            return false;
        }
        public override int GetHashCode() => content.GetHashCode();
        public override string ToString() => content;
        public static bool operator ==(WalContent left, WalContent right) => left.Equals(right);
        public static bool operator !=(WalContent left, WalContent right) => !(left == right);
    }

    public struct WalVersion : IFormattable
    {
        private readonly int version;

        public WalVersion(int version) => this.version = version;

        public Version ToVersion() => new(version, 0);
        public int ToInt32() => version;

        public static WalVersion? Create(int? version) => version.HasValue ? new WalVersion(version.Value) : null;
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is WalVersion wal)
                return wal.version == version;
            return false;
        }
        public override int GetHashCode() => version.GetHashCode();
        public override string ToString() => version.ToString();
        public string ToString([StringSyntax("NumericFormat")] string? format) => version.ToString(format);
        public string ToString([StringSyntax("NumericFormat")] string? format, IFormatProvider? provider) => version.ToString(format, provider);
        public static bool operator ==(WalVersion left, WalVersion right) => left.Equals(right);
        public static bool operator !=(WalVersion left, WalVersion right) => !(left == right);
        public static bool operator >(WalVersion left, WalVersion right) => left.version > right.version;
        public static bool operator <(WalVersion left, WalVersion right) => left.version < right.version;
        public static bool operator >=(WalVersion left, WalVersion right) => left.version >= right.version;
        public static bool operator <=(WalVersion left, WalVersion right) => left.version <= right.version;
    }

    public struct WalFileName
    {
        private readonly string name;

        public WalFileName(string name)
        {
            if (!name.EndsWith(WalFile.Extension))
                this.name = name + WalFile.Extension;
            else
                this.name = name;
        }

        public string WithoutExtension => name.Replace(WalFile.Extension, null);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null) return false;
            if (obj is WalFileName wal)
                return wal.name == name;
            return false;
        }
        public override int GetHashCode() => name.GetHashCode();
        public override string ToString() => name;
        public static implicit operator string(WalFileName source) => source.name;
        public static bool operator ==(WalFileName left, WalFileName right) => left.Equals(right);
        public static bool operator !=(WalFileName left, WalFileName right) => !(left == right);
    }

    public static class WalFileFactory
    {
        public static WalFile Create(FileInfo file, ScriptVersion version)
        {
            var wal = new WalFile(file, version);
            wal.Save();
            return wal;
        }
    }
}