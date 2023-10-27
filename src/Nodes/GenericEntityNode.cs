using System.Collections;
using System.Diagnostics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenWorldBuilder
{
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("GenericEntityNode")]
    public class GenericEntityNode : Node
    {
        private static Type[] fieldTypes = new Type[]
        {
            typeof(bool),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Color),
            typeof(string),
            typeof(string),
            typeof(Guid),
        };

        [JsonProperty]
        public Guid entityDefinition;

        public Dictionary<string, object> fields = new Dictionary<string, object>();

        public void SetDefinition(EntityDefinition def)
        {
            entityDefinition = def.guid;
            
            fields.Clear();

            foreach (var fieldDef in def.fields)
            {
                var fieldType = fieldTypes[(int)fieldDef.fieldType];
                if (fieldDef.isArray)
                {
                    var listType = typeof(List<>);
                    fieldType = listType.MakeGenericType(fieldType);
                }

                fields.Add(fieldDef.name, Activator.CreateInstance(fieldType)!);
            }
        }

        public override void OnDeserialize(JObject jObject)
        {
            base.OnDeserialize(jObject);

            if (jObject["fields"] is JObject fieldData && App.Instance!.ActiveProject.FindDefinition(entityDefinition) is EntityDefinition def)
            {
                // use entity definition to deserialize fields
                foreach (var field in def.fields)
                {
                    if (fieldData[field.name] is JToken serializedField)
                    {
                        var fieldType = fieldTypes[(int)field.fieldType];
                        if (field.isArray)
                        {
                            fieldType = typeof(List<>).MakeGenericType(fieldType);
                        }

                        fields.Add(field.name, serializedField.ToObject(fieldType)!);
                    }
                }
            }
        }

        public override void OnSerialize(JObject jObject)
        {
            base.OnSerialize(jObject);

            // just use default serialization
            jObject["fields"] = JObject.FromObject(fields);
        }

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.Draw(view, projection, viewport, selected);

            if (App.Instance!.ActiveProject.FindDefinition(entityDefinition) is EntityDefinition def)
            {
                foreach (var gizmo in def.gizmos)
                {
                    Matrix gizmoTransform = Matrix.CreateScale(gizmo.scale) * Matrix.CreateFromQuaternion(gizmo.rotation)
                        * Matrix.CreateTranslation(gizmo.position) * World;

                    switch (gizmo.shapeType)
                    {
                        case EntityGizmoShape.Box:
                            viewport.DrawBoxGizmo(gizmoTransform, gizmo.color);
                        break;
                        case EntityGizmoShape.Sphere:
                            viewport.DrawSphereGizmo(gizmoTransform.Translation, MathF.Max(MathF.Max(gizmo.scale.X, gizmo.scale.Y), gizmo.scale.Z), gizmo.color);
                        break;
                        case EntityGizmoShape.Line:
                            viewport.DrawLineGizmo(gizmoTransform.Translation,
                                Vector3.Transform(Vector3.UnitZ, gizmoTransform), gizmo.color, gizmo.color);
                        break;
                    }
                }
            }
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

            if (App.Instance!.ActiveProject.FindDefinition(entityDefinition) is EntityDefinition def)
            {
                // ensure all fields are present & are the correct type
                // TODO: this feels super inefficient, should probably work on some kind of version caching
                foreach (var fieldDef in def.fields)
                {
                    var fieldType = fieldTypes[(int)fieldDef.fieldType];
                    if (fieldDef.isArray)
                    {
                        var listType = typeof(List<>);
                        fieldType = listType.MakeGenericType(fieldType);
                    }

                    if (fields.TryGetValue(fieldDef.name, out var field))
                    {
                        if (field.GetType() != fieldType)
                        {
                            fields[fieldDef.name] = Activator.CreateInstance(fieldType)!;
                        }
                    }
                    else
                    {
                        fields.Add(fieldDef.name, Activator.CreateInstance(fieldType)!);
                    }

                    // edit field
                    if (fieldDef.isArray)
                    {
                        EditFieldList(fieldDef.name, fieldDef.fieldType, fields[fieldDef.name]);
                    }
                    else
                    {
                        object obj = fields[fieldDef.name];
                        EditFieldSingle(fieldDef.name, fieldDef.fieldType, ref obj);
                        fields[fieldDef.name] = obj;
                    }
                }
            }
            else
            {
                ImGui.Text($"ERROR - Invalid entity definition ({entityDefinition})");
            }
        }

        private void EditFieldList(string fieldName, EntityFieldType fieldType, object fieldData)
        {
            IList list = (IList)fieldData;
            Type innerType = fieldTypes[(int)fieldType];

            if (ImGui.CollapsingHeader($"{fieldName}"))
            {
                ImGui.Indent();
                ImGui.Text($"{list.Count} element(s)");
                for (int i = 0; i < list.Count; i++)
                {
                    object obj = list[i]!;
                    EditFieldSingle($"##edit_{fieldName}_{i}", fieldType, ref obj);
                    if (ImGui.Button($"Delete##{fieldName}_{i}"))
                    {
                        list.RemoveAt(i--);
                        continue;
                    }
                    list[i] = obj;
                }
                if (ImGui.Button($"Add##{fieldName}"))
                {
                    list.Add(Activator.CreateInstance(innerType));
                }
                ImGui.Unindent();
            }
        }

        private void EditFieldSingle(string fieldName, EntityFieldType fieldType, ref object fieldData)
        {
            switch (fieldType)
            {
                case EntityFieldType.Bool:
                    bool b = (bool)fieldData;
                    ImGui.Checkbox(fieldName, ref b);
                    fieldData = b;
                break;
                case EntityFieldType.Int:
                    int i = (int)fieldData;
                    ImGui.InputInt(fieldName, ref i);
                    fieldData = i;
                break;
                case EntityFieldType.Float:
                    float f = (float)fieldData;
                    ImGui.DragFloat(fieldName, ref f);
                    fieldData = f;
                break;
                case EntityFieldType.String:
                    string s = (string)fieldData;
                    ImGui.InputText(fieldName, ref s, 1024);
                    fieldData = s;
                break;
                case EntityFieldType.Vector2:
                    Vector2 v2 = (Vector2)fieldData;
                    ImGuiExt.DragFloat2(fieldName, ref v2);
                    fieldData = v2;
                break;
                case EntityFieldType.Vector3:
                    Vector3 v3 = (Vector3)fieldData;
                    ImGuiExt.DragFloat3(fieldName, ref v3);
                    fieldData = v3;
                break;
                case EntityFieldType.Vector4:
                    Vector4 v4 = (Vector4)fieldData;
                    ImGuiExt.DragFloat4(fieldName, ref v4);
                    fieldData = v4;
                break;
                case EntityFieldType.Color:
                    Color c = (Color)fieldData;
                    ImGuiExt.ColorEdit4(fieldName, ref c);
                    fieldData = c;
                break;
                case EntityFieldType.Quaternion:
                    ImGui.Text("TODO");
                break;
                case EntityFieldType.MultilineString:
                    ImGui.Text("TODO");
                break;
                case EntityFieldType.FilePath:
                    ImGui.Text("TODO");
                break;
                case EntityFieldType.EntityRef:
                    ImGui.Text("TODO");
                break;
            }
        }
    }
}