using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Joba.IBM.RPA
{
    class IncludeInternalPropertyJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var info = base.GetTypeInfo(type, options);

            if (info.Kind == JsonTypeInfoKind.Object)
            {
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).Where(p => (p.GetGetMethod(true)?.IsAssembly).GetValueOrDefault());
                foreach (var property in properties)
                {
                    var name = options.PropertyNamingPolicy?.ConvertName(property.Name) ?? property.Name;
                    var jsonProperty = info.CreateJsonPropertyInfo(property.PropertyType, name);
                    jsonProperty.Get = property.GetValue;
                    jsonProperty.Set = property.SetValue;
                    info.Properties.Add(jsonProperty);
                }
            }

            return info;
        }
    }
}