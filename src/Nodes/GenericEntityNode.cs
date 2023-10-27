using System.Collections;
using System.Diagnostics;
using ImGuiNET;
using Microsoft.Xna.Framework;
using NativeFileDialogSharp;
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
            typeof(string),
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

                fields.Add(fieldDef.name, CreateInstance(fieldType));
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

            ImGui.Spacing();

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
                            fields[fieldDef.name] = CreateInstance(fieldType);
                        }
                    }
                    else
                    {
                        fields.Add(fieldDef.name, CreateInstance(fieldType));
                    }

                    // edit field
                    if (fieldDef.isArray)
                    {
                        EditFieldList(fieldDef.name, fieldDef.fieldType, fields[fieldDef.name]);
                    }
                    else
                    {
                        object obj = fields[fieldDef.name];
                        EditFieldSingle(fieldDef.name, fieldDef.fieldType, ref obj, () => {
                            App.Instance!.BeginRecordUndo("Change " + fieldDef.name, () => {
                                fields[fieldDef.name] = obj;
                            });
                        }, () => {
                            App.Instance!.EndRecordUndo(() => {
                                fields[fieldDef.name] = obj;
                            });
                        });
                    }
                }
            }
            else
            {
                ImGui.Text($"ERROR - Invalid entity definition ({entityDefinition})");
            }
        }

        private object CreateInstance(Type type)
        {
            if (type == typeof(string))
            {
                return "";
            }

            return Activator.CreateInstance(type)!;
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
                    int idx = i;
                    EditFieldSingle($"Element {i}##edit_{fieldName}_{i}", fieldType, ref obj, () => {
                        App.Instance!.BeginRecordUndo("Change Array Element", () => {
                            list[idx] = obj;
                        });
                    }, () => {
                        App.Instance!.EndRecordUndo(() => {
                            list[idx] = obj;
                        });
                    });
                    if (ImGui.Button($"Delete##{fieldName}_{i}"))
                    {
                        App.Instance!.BeginRecordUndo("Remove Array Element", () => {
                            list.Insert(idx, obj);
                        });
                        App.Instance!.EndRecordUndo(() => {
                            list.RemoveAt(idx);
                        });
                        i--;
                        continue;
                    }
                    list[i] = obj;
                }
                if (ImGui.Button($"Add##{fieldName}"))
                {
                    App.Instance!.BeginRecordUndo("Add Array Element", () => {
                        list.RemoveAt(list.Count - 1);
                    });
                    App.Instance!.EndRecordUndo(() => {
                        list.Add(CreateInstance(innerType));
                    });
                }
                ImGui.Unindent();
            }
        }

        private void EditFieldSingle(string fieldName, EntityFieldType fieldType, ref object fieldData, Action start, Action end)
        {
            bool changed = false;

            switch (fieldType)
            {
                case EntityFieldType.Bool:
                    bool b = (bool)fieldData;
                    ImGui.Checkbox(fieldName, ref b);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = b;
                break;
                case EntityFieldType.Int:
                    int i = (int)fieldData;
                    ImGui.InputInt(fieldName, ref i);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = i;
                break;
                case EntityFieldType.Float:
                    float f = (float)fieldData;
                    ImGui.DragFloat(fieldName, ref f);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = f;
                break;
                case EntityFieldType.String:
                    string s = (string)fieldData;
                    ImGui.InputText(fieldName, ref s, 1024);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = s;
                break;
                case EntityFieldType.Vector2:
                    Vector2 v2 = (Vector2)fieldData;
                    ImGuiExt.DragFloat2(fieldName, ref v2);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = v2;
                break;
                case EntityFieldType.Vector3:
                    Vector3 v3 = (Vector3)fieldData;
                    ImGuiExt.DragFloat3(fieldName, ref v3);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = v3;
                break;
                case EntityFieldType.Vector4:
                    Vector4 v4 = (Vector4)fieldData;
                    ImGuiExt.DragFloat4(fieldName, ref v4);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = v4;
                break;
                case EntityFieldType.Color:
                    Color c = (Color)fieldData;
                    ImGuiExt.ColorEdit4(fieldName, ref c);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = c;
                break;
                case EntityFieldType.Quaternion:
                    Quaternion q = (Quaternion)fieldData;
                    Vector3 euler = MathUtils.ToEulerAngles(q);
                    euler = MathUtils.ToDegrees(euler);
                    if (ImGuiExt.DragFloat3(fieldName, ref euler))
                    {
                        euler = MathUtils.ToRadians(euler);
                        q = MathUtils.ToQuaternion(euler);
                    }
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = q;
                break;
                case EntityFieldType.MultilineString:
                    string s2 = (string)fieldData;
                    float widgetWidth = ImGui.CalcItemWidth();
                    float widgetHeight = ImGui.GetTextLineHeightWithSpacing();
                    ImGui.InputTextMultiline(fieldName, ref s2, 8192, new System.Numerics.Vector2(widgetWidth, widgetHeight * 4f));
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    fieldData = s2;
                break;
                case EntityFieldType.FilePath:
                    string s3 = (string)fieldData;
                    changed = false;
                    ImGui.InputText(fieldName, ref s3, 1024);
                    if (ImGui.IsItemActivated())
                    {
                        start.Invoke();
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        end.Invoke();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Browse##" + fieldName))
                    {
                        var fileResult = Dialog.FileOpen(null, App.Instance!.ProjectFolder);
                        if (fileResult.IsOk)
                        {
                            // make relative to project folder
                            s3 = Path.GetRelativePath(App.Instance!.ProjectFolder!, fileResult.Path);
                            changed = true;
                        }
                    }
                    if (changed) start.Invoke();
                    fieldData = s3;
                    if (changed) end.Invoke();
                break;
                case EntityFieldType.NodeRef:
                    string n = (string)fieldData;
                    changed = false;
                    if (n == "")
                    {
                        ImGui.LabelText(fieldName, "(none)");
                    }
                    else
                    {
                        var node = Scene?.FindChildByGuid(Guid.Parse(n));
                        if (node == null)
                        {
                            ImGui.LabelText(fieldName, "(invalid reference)");
                        }
                        else
                        {
                            ImGui.LabelText(fieldName, node.name);
                        }
                    }
                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("NODE");
                        unsafe
                        {
                            if (payload.NativePtr != null)
                            {
                                Node payloadNode = (Node)App.dragPayload!;
                                Console.WriteLine("ACCEPT NODE: " + payloadNode.name);
                                n = payloadNode.guid.ToString();
                                changed = true;
                            }
                        }
                        ImGui.EndDragDropTarget();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Clear##" + fieldName))
                    {
                        n = "";
                        changed = true;
                    }
                    if (changed) start.Invoke();
                    fieldData = n;
                    if (changed) end.Invoke();
                break;
            }
        }
    }
}