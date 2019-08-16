using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dfc.ProviderPortal.ChangeFeedListener.Helpers
{
    public class AzureSearchProviderModelJsonConverter : JsonConverter
    {
        // Mappings defining overrides during property deserialization.
        // - override ProviderName if CourseDirectoryName has a non-null value (COUR-1491)
        private readonly Dictionary<string, string> _propertyOverrideMappings = new Dictionary<string, string>
        {
            {"ProviderName", "CourseDirectoryName"}
        };

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().IsClass;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Perform default deserialization.
            object instance = Activator.CreateInstance(objectType);
            JObject jsonObject = JObject.Load(reader);
            serializer.Populate(jsonObject.CreateReader(), instance);

            // Apply the overrides - update the target field with the value of the source field if that source value is not null.
            var sourceProps = jsonObject.Properties();
            var targetProps = objectType.GetTypeInfo().DeclaredProperties.ToList();
            foreach (var pom in _propertyOverrideMappings)
            {
                PropertyInfo targetProp = targetProps.FirstOrDefault(pi => pi.CanWrite && pi.Name == pom.Key);
                if (null != targetProp)
                {
                    var sourceProp = sourceProps.FirstOrDefault(p => p.Name == pom.Value);
                    if (null != sourceProp)
                    {
                        var value = sourceProp.Value.ToObject(targetProp.PropertyType, serializer);
                        if (null != value)
                        {
                            targetProp.SetValue(instance, value);
                        }
                    }
                }
            }

            return instance;
        }
    }
}
