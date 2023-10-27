using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public enum EntityFieldType
    {
        Bool,
        Int,
        Float,
        String,
        Vector2,
        Vector3,
        Vector4,
        Quaternion,
        Color,
        MultilineString,
        FilePath,
        NodeRef,
    }

    public enum EntityGizmoShape
    {
        Box,
        Sphere,
        Line,
    }

    public struct EntityGizmo
    {
        public EntityGizmoShape shapeType;
        public Color color;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public struct EntityFieldDefinition
    {
        public string name;
        public EntityFieldType fieldType;
        public bool isArray;
    }

    public struct EntityDefinition
    {
        public Guid guid;
        public string name;
        public List<EntityFieldDefinition> fields;
        public List<EntityGizmo> gizmos;
    }

    public class Project
    {
        public string contentPath = "";
        public List<EntityDefinition> entityDefinitions = new List<EntityDefinition>();

        public EntityDefinition? FindDefinition(Guid guid)
        {
            foreach (var def in entityDefinitions)
            {
                if (def.guid.Equals(guid))
                {
                    return def;
                }
            }

            return null;
        }
    }
}