namespace Joba.IBM.RPA
{
    public readonly struct ServerConfig
    {
        private readonly IDictionary<string, Region> regions;

        public ServerConfig()
        {
            regions = new Dictionary<string, Region>();
            Version = new Version("0.0.0");
        }

        public Region[] Regions
        {
            get => regions.Values.ToArray();
            init => regions = value.ToDictionary(d => d.Name, v => v);
        }

        public AuthenticationMethod AuthenticationMethod { get; init; }
        [JsonPropertyName("ProductEnvironment")]
        public DeploymentOption Deployment { get; init; }
        [JsonPropertyName("SetupVersion")]
        public Version Version { get; init; }

        public Region? GetByName(string name)
        {
            regions.TryGetValue(name, out var region);
            return region;
        }

        public void EnsureValid(Version supportedServerVersion)
        {
            if (regions.Count == 0)
                throw new NotSupportedException("The server has no regions.");
            if (Version < supportedServerVersion)
                throw new NotSupportedException($"The server version {Version} is not supported. It must be greater or equal to {supportedServerVersion}.");
            if (!AuthenticationMethod.IsSupported)
                throw new NotSupportedException($"The server authentication method '{AuthenticationMethod}' is not supported.");
            if (!Deployment.IsSupported)
                throw new NotSupportedException($"The server deployment option '{Deployment}' is not supported.");
        }
    }

    public readonly struct AuthenticationMethod
    {
        public static readonly AuthenticationMethod WDG = new(nameof(WDG));
        public static readonly AuthenticationMethod OIDC = new(nameof(OIDC));
        private readonly string method;

        internal AuthenticationMethod(string method) => this.method = method;
        internal bool IsSupported => method == WDG || method == OIDC;
        public override string ToString() => method;
        public static implicit operator string(AuthenticationMethod method) => method.ToString();
    }

    public readonly struct DeploymentOption
    {
        public static readonly DeploymentOption SaaS = new(nameof(SaaS));
        public static readonly DeploymentOption OnPrem = new(nameof(OnPrem));
        public static readonly DeploymentOption OCP = new(nameof(OCP));
        private readonly string option;

        internal DeploymentOption(string option) => this.option = option;
        internal bool IsSupported => option == SaaS || option == OnPrem || option == OCP;
        public override string ToString() => option;
        public static implicit operator string(DeploymentOption option) => option.ToString();
    }
}
