using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Joba.IBM.RPA
{
    class IncludeInternalMembersJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                ConfigureInternalParameterlessConstructor(info);
                ConfigureInternalProperties(info, options);
            }

            return info;
        }

        private static void ConfigureInternalParameterlessConstructor(JsonTypeInfo info)
        {
            var ctor = info.Type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);
            if (ctor != null)
                info.CreateObject = () => ctor.Invoke(null);
        }

        private static void ConfigureInternalProperties(JsonTypeInfo info, JsonSerializerOptions options)
        {
            var properties = info.Type
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(p => (p.GetGetMethod(true)?.IsAssembly).GetValueOrDefault());

            foreach (var property in properties)
            {
                var name = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                var jsonProperty = info.CreateJsonPropertyInfo(property.PropertyType, name);
                jsonProperty.Get = property.GetValue;
                jsonProperty.Set = property.SetValue;
                info.Properties.Add(jsonProperty);
            }
        }
    }
}