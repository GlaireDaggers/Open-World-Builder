using ImGuiNET;
using Microsoft.Xna.Framework;
using NativeFileDialogSharp;

namespace OpenWorldBuilder
{
    public class ProjectSettingsWindow : EditorWindow
    {
        private static string[] fieldTypeNames = new string[]
        {
            "Bool",
            "Int",
            "Float",
            "String",
            "Vector2",
            "Vector3",
            "Vector4",
            "Rotation",
            "Color",
            "Multiline String",
            "File Path",
            "Node Ref",
        };

        private string _newEntityDefName = "";
        private string _newFieldName = "";

        public ProjectSettingsWindow() : base()
        {
            title = "Project Settings";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            if (App.Instance!.ProjectPath == null)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.InputText("Content Path", ref App.Instance!.ActiveProject.contentPath, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                App.Instance!.SetContentPath(App.Instance!.ActiveProject.contentPath);
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Browse"))
            {
                var result = Dialog.FolderPicker(App.Instance!.ActiveProject.contentPath);
                if (result.IsOk)
                {
                    string relPath = Path.GetRelativePath(App.Instance!.ProjectFolder!, result.Path);
                    App.Instance!.SetContentPath(relPath);
                }
            }

            if (ImGui.CollapsingHeader("Entity Definitions"))
            {
                ImGui.Indent();
                ImGui.Text($"{App.Instance!.ActiveProject.entityDefinitions.Count} definition(s)");

                for (int i = 0; i < App.Instance!.ActiveProject.entityDefinitions.Count; i++)
                {
                    var def = App.Instance!.ActiveProject.entityDefinitions[i];
                    {
                        if (ImGui.CollapsingHeader($"{def.name} ({def.guid})"))
                        {
                            ImGui.Indent();
                            EditEntityDefinition(ref def);
                            ImGui.Unindent();

                            if (ImGui.Button($"Delete {def.name}##{def.guid}"))
                            {
                                App.Instance!.ActiveProject.entityDefinitions.RemoveAt(i--);
                                continue;                                
                            }
                        }
                    }
                    App.Instance!.ActiveProject.entityDefinitions[i] = def;
                }

                ImGui.InputText("##new_entity_name", ref _newEntityDefName, 1024);
                ImGui.SameLine();
                if (ImGui.Button("Add New##entity") && !string.IsNullOrEmpty(_newEntityDefName))
                {
                    App.Instance!.ActiveProject.entityDefinitions.Add(new EntityDefinition
                    {
                        guid = Guid.NewGuid(),
                        name = _newEntityDefName,
                        fields = new List<EntityFieldDefinition>(),
                        gizmos = new List<EntityGizmo>(),
                    });

                    _newEntityDefName = "";
                }

                ImGui.Unindent();
            }

            if (App.Instance!.ProjectPath == null)
            {
                ImGui.EndDisabled();
            }
        }

        private void EditEntityDefinition(ref EntityDefinition def)
        {
            ImGui.InputText($"Name##{def.guid}", ref def.name, 1024);

            if (ImGui.CollapsingHeader($"Fields##{def.guid}"))
            {
                ImGui.Text($"{def.fields.Count} field(s)");

                ImGui.Indent();
                for (int i = 0; i < def.fields.Count; i++)
                {
                    var field = def.fields[i];
                    int fieldType = (int)field.fieldType;
                    ImGui.InputText($"##fieldname_{def.guid}_{i}", ref field.name, 1024);
                    ImGui.SameLine();
                    ImGui.Combo($"##fieldtype_{def.guid}_{i}", ref fieldType, fieldTypeNames, fieldTypeNames.Length);
                    
                    ImGui.Checkbox($"Is Array##{def.guid}_{i}", ref field.isArray);
                    field.fieldType = (EntityFieldType)fieldType;
                    def.fields[i] = field;

                    if (ImGui.Button($"Delete Field##{def.guid}_{i}"))
                    {
                        def.fields.RemoveAt(i--);
                    }
                }

                ImGui.InputText($"##new_field_name_{def.guid}", ref _newFieldName, 1024);
                ImGui.SameLine();
                if (ImGui.Button($"Add New##field_{def.guid}") && !string.IsNullOrEmpty(_newFieldName))
                {
                    def.fields.Add(new EntityFieldDefinition
                    {
                        name = _newFieldName,
                        fieldType = EntityFieldType.Bool,
                        isArray = false
                    });
                    _newFieldName = "";
                }
                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader($"Gizmos##{def.guid}"))
            {
                ImGui.Text($"{def.gizmos.Count} gizmo(s)");

                ImGui.Indent();
                for (int i = 0; i < def.gizmos.Count; i++)
                {
                    var gizmo = def.gizmos[i];
                    int shapeType = (int)gizmo.shapeType;
                    
                    ImGui.Combo($"Shape##{def.guid}_{i}", ref shapeType, "Box\0Sphere\0Line");
                    ImGuiExt.ColorEdit4($"Color##{def.guid}_{i}", ref gizmo.color);
                    ImGuiExt.DragFloat3($"Position##{def.guid}_{i}", ref gizmo.position);
                    
                    Vector3 euler = MathUtils.ToEulerAngles(gizmo.rotation);
                    euler = MathUtils.ToDegrees(euler);
                    if (ImGuiExt.DragFloat3($"Rotation##{def.guid}_{i}", ref euler))
                    {
                        euler = MathUtils.ToRadians(euler);
                        gizmo.rotation = MathUtils.ToQuaternion(euler);
                    }
                    
                    ImGuiExt.DragFloat3($"Scale##{def.guid}_{i}", ref gizmo.scale);
                    
                    gizmo.shapeType = (EntityGizmoShape)shapeType;
                    def.gizmos[i] = gizmo;
                    
                    if (ImGui.Button($"Delete Gizmo##{def.guid}_{i}"))
                    {
                        def.gizmos.RemoveAt(i--);
                    }
                }
                if (ImGui.Button($"Add New##gizmo_{def.guid}"))
                {
                    def.gizmos.Add(new EntityGizmo
                    {
                        color = Color.White,
                        shapeType = EntityGizmoShape.Box,
                        position = Vector3.Zero,
                        rotation = Quaternion.Identity,
                        scale = Vector3.One
                    });
                }
                ImGui.Unindent();
            }
        }
    }
}