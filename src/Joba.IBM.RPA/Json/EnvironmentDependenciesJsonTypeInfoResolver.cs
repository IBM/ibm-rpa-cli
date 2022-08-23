using System.Text.Json.Serialization.Metadata;

namespace Joba.IBM.RPA
{
    class EnvironmentDependenciesJsonTypeInfoResolver : IncludeInternalMembersJsonTypeInfoResolver
    {
        private readonly DirectoryInfo environmentDirectory;

        public EnvironmentDependenciesJsonTypeInfoResolver(DirectoryInfo environmentDirectory)
        {
            this.environmentDirectory = environmentDirectory;
        }

        protected override void ConfigureConstructor(JsonTypeInfo info)
        {
            if (typeof(IEnvironmentDependencies).IsAssignableFrom(info.Type))
                info.CreateObject = () => new EnvironmentDependencies(environmentDirectory);
        }
    }
}