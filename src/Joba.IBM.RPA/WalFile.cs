using ProtoBuf;

namespace Joba.IBM.RPA
{
    class WalFile
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

        public FileInfo Info { get; init; }
        private string Content { get; set; }
        private Guid? Id { get; set; }
        private Guid? VersionId { get; set; }
        private int? Version { get; set; }
        private Version ProductVersion { get; set; }

        public async Task UpdateToLatestAsync(IScriptClient client, CancellationToken cancellation)
        {
            var version = await client.GetLatestVersionAsync(Id!.Value, cancellation);
            if (version == null)
                throw new Exception($"Could not find the latest version of {Info.Name}");

            var content = await client.GetContentAsync(version.Id, cancellation);

            Content = content;
            Id = version.ScriptId;
            VersionId = version.Id;
            Version = version.Version;
            ProductVersion = System.Version.Parse(version.ProductVersion);

            Save();
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

        public static WalFile Read(FileInfo file)
        {
            using var stream = File.OpenRead(file.FullName);
            var proto = Serializer.Deserialize<WalFileProto>(stream);
            return new WalFile(file, proto);
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