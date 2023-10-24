namespace OpenWorldBuilder
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SerializedNodeAttribute : Attribute
    {
        public readonly string typeName;

        public SerializedNodeAttribute(string typeName)
        {
            this.typeName = typeName;
        }
    }
}