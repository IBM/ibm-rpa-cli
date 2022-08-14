using ProtoBuf;

namespace Joba.IBM.RPA
{
    public class WalFile
    {
        public static readonly string Extension = ".wal";

        private WalFile(FileInfo file, WalFileProto proto)
        {
            Info = file;
            Content = proto.Content;
            Id = proto.Id;
            VersionId = proto.VersionId;
            Version = proto.Version;
            ProductVersion = new Version(proto.ProductVersion);
        }

        internal WalFile(FileInfo file, ScriptVersion version)
        {
            Info = file;
            UpdateWith(version);
        }

        public FileInfo Info { get; init; }
        public bool IsFromServer => Id.HasValue;
        private string Content { get; set; }
        private Guid? Id { get; set; }
        private Guid? VersionId { get; set; }
        public int? Version { get; set; }
        private Version? ProductVersion { get; set; }

        public async Task UpdateToLatestAsync(IScriptClient client, CancellationToken cancellation)
        {
            if (!IsFromServer)
                throw new Exception($"The wal file '{Info.Name}' has not been downloaded from the server");

            var version = await client.GetLatestVersionAsync(Id!.Value, cancellation);
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

        public async Task OverwriteToLatestAsync(IScriptClient client, string fileName, CancellationToken cancellation)
        {
            var version = await client.GetLatestVersionAsync(Path.GetFileNameWithoutExtension(fileName), cancellation);
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

        private void UpdateWith(ScriptVersion version)
        {
            Content = version.Content;
            Id = version.ScriptId;
            VersionId = version.Id;
            Version = version.Version;
            ProductVersion = version.ProductVersion;
        }

        private void Save()
        {
            using var stream = File.OpenWrite(Info.FullName);
            var proto = new WalFileProto
            {
                Content = Content,
                Id = Id,
                ProductVersion = ProductVersion.ToString(),
                Version = Version,
                VersionId = VersionId
            };
            Serializer.Serialize(stream, proto);
        }

        internal static WalFile Read(FileInfo file)
        {
            using var stream = File.OpenRead(file.FullName);
            var proto = Serializer.Deserialize<WalFileProto>(stream);
            return new WalFile(file, proto);
        }

        internal static WalFile Create(FileInfo file, ScriptVersion version)
        {
            var wal = new WalFile(file, version);
            wal.Save();
            return wal;
        }

        [ProtoContract]
        class WalFileProto
        {
            [ProtoMember(1)]
            public Guid? Id { get; set; }

            [ProtoMember(2)]
            public string Content { get; set; }

            [ProtoMember(3)]
            public int? Version { get; set; }

            [ProtoMember(4)]
            public Guid? VersionId { get; set; }

            [ProtoMember(5)]
            public string ProductVersion { get; set; }
        }
    }
}