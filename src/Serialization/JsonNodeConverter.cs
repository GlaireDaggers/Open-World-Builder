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
            return DeserializeNode(objectType, serializer, jObj);
        }

        public override void WriteJson(JsonWriter writer, Node? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            JObject jObj = SerializeNode(value);
            jObj.WriteTo(writer);
        }

        private Node DeserializeNode(Type objectType, JsonSerializer serializer, JObject jObj)
        {
            Type targetType = GetNodeType(objectType, jObj);
            
            Node target = (Node)Activator.CreateInstance(targetType)!;
            serializer.Populate(jObj.CreateReader(), target);

            var childArr = (JArray)jObj.Property("children")!.Value;
            foreach (var childTok in childArr)
            {
                var childNode = DeserializeNode(typeof(Node), serializer, (JObject)childTok);
                target.AddChild(childNode);
            }

            return target;
        }

        private JObject SerializeNode(Node node)
        {
            JToken t = JToken.FromObject(node);
            JObject jObj = (JObject)t;
            
            if (node.GetType().GetCustomAttribute<SerializedNodeAttribute>() is SerializedNodeAttribute attr)
            {
                jObj.Add("type", (JToken)attr.typeName);
            }

            var childArr = new JArray();

            foreach (var childNode in node.Children)
            {
                childArr.Add(SerializeNode(childNode));
            }

            jObj.Add("children", childArr);
            return jObj;
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