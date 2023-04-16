using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using System.Linq;

namespace Joba.IBM.RPA
{
    class IncludeInternalMembersJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                ConfigureConstructor(info);
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

        private static void ConfigureConstructor(JsonTypeInfo info)
        {
            var ctor = info.Type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
            if (ctor != null)
                info.CreateObject = () => ctor.Invoke(null);
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