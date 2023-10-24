using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenWorldBuilder
{
    public class JsonNodeConverter : JsonConverter<Node>
    {
        private static Dictionary<string, Type>? typeCache = null;
        
        public override Node? ReadJson(JsonReader reader, Type objectType, Node? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jObj = JObject.Load(reader);
            Type targetType = GetNodeType(objectType, jObj);
            
            object target = Activator.CreateInstance(targetType)!;
            serializer.Populate(jObj.CreateReader(), target);

            return (Node)target;
        }

        public override void WriteJson(JsonWriter writer, Node? value, JsonSerializer serializer)
        {
            JObject jObj = new JObject();
            var objWriter = jObj.CreateWriter();
            serializer.Serialize(objWriter, value);
            
            if (value != null && value.GetType().GetCustomAttribute<SerializedNodeAttribute>() is SerializedNodeAttribute attr)
            {
                jObj.Add("type", (JToken)attr.typeName);
            }

            jObj.WriteTo(writer);
        }

        private Type GetNodeType(Type objType, JObject jObject)
        {
            var typeProp = (string?)jObject.Property("type");

            if (typeProp == null)
            {
                return objType;
            }

            if (typeCache == null)
            {
                // gather all classes with SerializedNodeAttribute
                typeCache = new Dictionary<string, Type>();

                var curAsm = Assembly.GetExecutingAssembly();
                BuildTypeCache(curAsm);

                foreach (var asmName in curAsm.GetReferencedAssemblies())
                {
                    var asm = Assembly.Load(asmName);
                    BuildTypeCache(asm);
                }
            }

            // try and retrieve type from cache
            if (typeCache.TryGetValue(typeProp, out var type))
            {
                return type;
            }

            Console.WriteLine("WARNING: Failed to resolve node type: " + typeProp);
            return objType;
        }

        private static void BuildTypeCache(Assembly asm)
        {
            foreach (var t in asm.GetTypes())
            {
                if (typeof(Node).IsAssignableFrom(t) && t.GetCustomAttribute<SerializedNodeAttribute>() is SerializedNodeAttribute attr)
                {
                    typeCache!.Add(attr.typeName, t);
                }
            }
        }
    }
}