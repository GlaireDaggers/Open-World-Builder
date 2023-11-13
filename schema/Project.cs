namespace OWB
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public enum FieldType
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
        NodeRef
    }
    
    public enum ShapeType
    {
        Box,
        Sphere,
        Line
    }

    public partial class Project
    {
        [JsonProperty("contentPath")]
        public string ContentPath { get; set; }

        [JsonProperty("entityDefinitions")]
        public EntityDefinition[] EntityDefinitions { get; set; }
    }

    public partial class EntityDefinition
    {
        [JsonProperty("fields")]
        public Field[] Fields { get; set; }

        [JsonProperty("gizmos")]
        public Gizmo[] Gizmos { get; set; }

        [JsonProperty("guid")]
        public Guid Guid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Field
    {
        [JsonProperty("fieldType")]
        public FieldType FieldType { get; set; }

        [JsonProperty("isArray")]
        public bool IsArray { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public partial class Gizmo
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("rotation")]
        public string Rotation { get; set; }

        [JsonProperty("scale")]
        public string Scale { get; set; }

        [JsonProperty("shapeType")]
        public ShapeType ShapeType { get; set; }
    }
}

