using ProtoBuf;
using System.Diagnostics.CodeAnalysis;

namespace Joba.IBM.RPA
{
    public class WalFile
    {
        public static readonly string Extension = ".wal";

        protected WalFile(FileInfo file, WalFileProto proto)
        {
            Info = file;
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
            UpdateWith(version);
        }

        public FileInfo Info { get; }
        public string Name => Path.GetFileNameWithoutExtension(Info.Name);
        public bool IsFromServer => Id.HasValue;
        protected internal WalContent Content { get; private set; }
        protected Guid? Id { get; set; }
        protected Guid? VersionId { get; set; }
        public WalVersion? Version { get; set; }
        protected Version? ProductVersion { get; set; }

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

        internal void Overwrite(WalContent content)
        {
            Content = content;
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

        internal protected virtual void Save()
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

        internal virtual WalFile Clone()
        {
            var proto = new WalFileProto { Content = Content.ToString(), Id = Id, ProductVersion = ProductVersion?.ToString(), Version = Version?.ToInt32(), VersionId = VersionId };
            return new WalFile(Info, proto);
        }

        public override string ToString() => Content.ToString();

        internal static WalFile Read(FileInfo file)
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

    public class PackageFile : WalFile
    {
        internal PackageFile(FileInfo file, ScriptVersion version)
            : base(file, version) { }

        protected PackageFile(FileInfo file, WalFileProto proto)
            : base(file, proto) { }

        internal protected override void Save()
        {
            try
            {
                //TODO: hide file (not working)
                //if (Info.Exists)
                //{
                //    Info.IsReadOnly = false;
                //    Info.Refresh();
                //}
                base.Save();
            }
            //catch (Exception ex) { }
            finally
            {
                //Info.IsReadOnly = true;
            }
        }

        internal override WalFile Clone()
        {
            var proto = new WalFileProto { Content = Content.ToString(), Id = Id, ProductVersion = ProductVersion?.ToString(), Version = Version?.ToInt32(), VersionId = VersionId };
            return new PackageFile(Info, proto);
        }
    }

    public static class WalFileFactory
    {
        public static WalFile Create(FileInfo file, ScriptVersion version)
        {
            var wal = new WalFile(file, version);
            wal.Save();
            return wal;
        }

        public static PackageFile CreateAsPackage(FileInfo file, ScriptVersion version)
        {
            var wal = new PackageFile(file, version);
            wal.Save();
            return wal;
        }
    }
}