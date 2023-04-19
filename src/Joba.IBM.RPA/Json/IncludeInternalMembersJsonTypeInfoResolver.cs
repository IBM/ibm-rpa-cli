using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Joba.IBM.RPA
{
    class IncludeInternalMembersJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        private readonly DirectoryInfo? workingDirectory;

        /// <summary>
        /// TODO: didn't like this. Rethink.
        /// </summary>
        public IncludeInternalMembersJsonTypeInfoResolver(DirectoryInfo? workingDirectory = null)
        {
            this.workingDirectory = workingDirectory;
        }

        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                ConfigureConstructor(info, workingDirectory);
                ConfigureProperties(info, options);
            }

            SkipEmptyCollections(info);
            return info;
        }

        private static void SkipEmptyCollections(JsonTypeInfo info)
        {
            foreach (var property in info.Properties.Where(property => typeof(ICollection).IsAssignableFrom(property.PropertyType)))
                property.ShouldSerialize = (_, value) => value is ICollection collection && collection.Count > 0;
        }

        private static void ConfigureConstructor(JsonTypeInfo info, DirectoryInfo? workingDirectory = null)
        {
            if (workingDirectory != null)
            {
                var ctor = info.Type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, new Type[] { typeof(DirectoryInfo) });
                if (ctor != null)
                    info.CreateObject = () => ctor.Invoke(new object[] { workingDirectory });
                else
                {
                    ctor = info.Type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
                    if (ctor != null)
                        info.CreateObject = () => ctor.Invoke(null);
                }
            }
            else
            {
                var ctor = info.Type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
                if (ctor != null)
                    info.CreateObject = () => ctor.Invoke(null);
            }
        }

        private static void ConfigureProperties(JsonTypeInfo info, JsonSerializerOptions options)
        {
            var properties = info.Type
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(p => (p.GetGetMethod(true)?.IsAssembly).GetValueOrDefault());

            foreach (var property in properties)
            {
                var name = GetMemberName(property, options);
                var jsonProperty = info.CreateJsonPropertyInfo(property.PropertyType, name);
                jsonProperty.Get = property.GetValue;
                jsonProperty.Set = property.SetValue;
                info.Properties.Add(jsonProperty);
            }
        }

        private static string GetMemberName(MemberInfo memberInfo, JsonSerializerOptions options)
        {
            var nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false);
            if (nameAttribute != null)
                return nameAttribute.Name;

            return options.PropertyNamingPolicy?.ConvertName(memberInfo.Name) ?? memberInfo.Name;
        }
    }
}