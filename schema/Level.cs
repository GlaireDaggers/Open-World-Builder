namespace OWB
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class Project
    {
        [JsonProperty("root")]
        public SceneNode Root { get; set; }
    }
    
    public partial class Node
    {
        [JsonProperty("children")]
        public Node[] Children { get; set; }

        [JsonProperty("guid")]
        public Guid Guid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("rotation")]
        public string Rotation { get; set; }

        [JsonProperty("scale")]
        public string Scale { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class SceneNode : Node
    {
        [JsonProperty("ambientColor")]
        public string AmbientColor { get; set; }

        [JsonProperty("ambientIntensity")]
        public double AmbientIntensity { get; set; }
    }

    public partial class GenericEntityNode : Node
    {
        [JsonProperty("entityDefinition")]
        public Guid EntityDefinition { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, object> Fields { get; set; }
    }

    public partial class LightNode : Node
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("innerConeAngle")]
        public double InnerConeAngle { get; set; }

        [JsonProperty("intensity")]
        public double Intensity { get; set; }

        [JsonProperty("lightType")]
        public long LightType { get; set; }

        [JsonProperty("outerConeAngle")]
        public double OuterConeAngle { get; set; }

        [JsonProperty("radius")]
        public double Radius { get; set; }
    }

    public partial class SplineNode : Node
    {
        [JsonProperty("closed")]
        public bool Closed { get; set; }

        [JsonProperty("points")]
        public SplineControlPoint[] Points { get; set; }
    }

    public partial class StaticMeshNode : Node
    {
        [JsonProperty("collision")]
        public long Collision { get; set; }

        [JsonProperty("meshPath")]
        public string MeshPath { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }
    }

    public partial class BrushNode : Node
    {
        [JsonProperty("collision")]
        public long Collision { get; set; }

        [JsonProperty("planes")]
        public BrushPlane[] Planes { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }
    }

	public partial class SplineControlPoint
    {
        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("rotation")]
        public string Rotation { get; set; }

        [JsonProperty("scale")]
        public double Scale { get; set; }
    }

    public partial class BrushPlane
    {
        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("rotation")]
        public string Rotation { get; set; }

        [JsonProperty("textureOffset")]
        public string TextureOffset { get; set; }

        [JsonProperty("texturePath")]
        public string TexturePath { get; set; }

        [JsonProperty("textureScale")]
        public string TextureScale { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }
    }
}
