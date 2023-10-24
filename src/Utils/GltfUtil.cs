using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;

namespace OpenWorldBuilder
{
    public struct GltfMeshPart : IDisposable
    {
        public Matrix transform;
        public int materialIdx;
        public VertexBuffer vb;
        public IndexBuffer ib;

        public void Dispose()
        {
            vb.Dispose();
            ib.Dispose();
        }
    }

    public class GltfMesh : IDisposable
    {
        public List<GltfMeshPart> meshParts = new List<GltfMeshPart>();

        public void Dispose()
        {
            foreach (var part in meshParts)
            {
                part.Dispose();
            }
        }
    }

    public class GltfModel : IDisposable
    {
        public struct ModelNode
        {
            public int meshIdx;
            public Matrix transform;
        }

        public List<Texture2D> textures = new List<Texture2D>();
        public List<BasicEffect> materials = new List<BasicEffect>();
        public List<GltfMesh> meshes = new List<GltfMesh>();
        public List<ModelNode> nodes = new List<ModelNode>();

        public void Dispose()
        {
            foreach (var tex in textures)
            {
                tex.Dispose();
            }

            foreach (var material in materials)
            {
                material.Dispose();
            }

            foreach (var mesh in meshes)
            {
                mesh.Dispose();
            }

            textures.Clear();
            materials.Clear();
            meshes.Clear();
        }
    }

    public static class GltfUtil
    {
        private static VertexDeclaration MeshVertDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(32, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );

        private struct MeshVert
        {
            public Vector3 pos;
            public Vector3 normal;
            public Vector2 uv;
            public Color col;
        }

        public static GltfModel ConvertGltf(string path, GraphicsDevice gd)
        {
            var model = ModelRoot.Load(path);

            List<Texture2D> textures = new List<Texture2D>();
            List<BasicEffect> materials = new List<BasicEffect>();

            foreach (var mat in model.LogicalMaterials)
            {
                var fx = new BasicEffect(gd);
                fx.TextureEnabled = false;
                fx.VertexColorEnabled = true;
                fx.LightingEnabled = true;

                if (mat.FindChannel("BaseColor") is MaterialChannel baseColor)
                {
                    if (baseColor.Texture != null)
                    {
                        var tex = Texture2D.FromStream(gd, baseColor.Texture.PrimaryImage.Content.Open());
                        textures.Add(tex);

                        textures.Add(tex);
                        fx.Texture = tex;
                        fx.TextureEnabled = true;
                    }

                    fx.DiffuseColor = new Vector3(baseColor.Color.X, baseColor.Color.Y, baseColor.Color.Z);
                }

                materials.Add(fx);
            }

            List<GltfMesh> meshes = new List<GltfMesh>();
            foreach (var mesh in model.LogicalMeshes)
            {
                var prims = new List<GltfMeshPart>();
                foreach (var prim in mesh.Primitives)
                {
                    var vpos = prim.GetVertexAccessor("POSITION")?.AsVector3Array()!;
                    var vnorm = prim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                    var vcolor = prim.GetVertexAccessor("COLOR_0")?.AsVector4Array();
                    var vtex = prim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                    var tris = prim.GetTriangleIndices().ToArray();

                    MeshVert[] vertices = new MeshVert[vpos.Count];
                    uint[] indices = new uint[tris.Length * 3];

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        MeshVert v = new MeshVert
                        {
                            pos = new Vector3(vpos[i].X, vpos[i].Y, vpos[i].Z),
                            col = Color.White
                        };

                        if (vnorm != null) v.normal = new Vector3(vnorm[i].X, vnorm[i].Y, vnorm[i].Z);
                        if (vcolor != null) v.col = new Color(vcolor[i].X, vcolor[i].Y, vcolor[i].Z, vcolor[i].W);
                        if (vtex != null) v.uv = new Vector2(vtex[i].X, vtex[i].Y);

                        vertices[i] = v;
                    }

                    for (int i = 0; i < tris.Length; i++)
                    {
                        indices[i * 3] = (uint)tris[i].A;
                        indices[(i * 3) + 1] = (uint)tris[i].B;
                        indices[(i * 3) + 2] = (uint)tris[i].C;
                    }

                    VertexBuffer vb = new VertexBuffer(gd, MeshVertDeclaration, vertices.Length, BufferUsage.WriteOnly);
                    IndexBuffer ib = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);

                    vb.SetData(vertices);
                    ib.SetData(indices);

                    prims.Add(new GltfMeshPart
                    {
                        materialIdx = prim.Material.LogicalIndex,
                        vb = vb,
                        ib = ib,
                    });
                }

                meshes.Add(new GltfMesh
                {
                    meshParts = prims
                });
            }

            List<GltfModel.ModelNode> nodes = new List<GltfModel.ModelNode>();
            foreach (var node in model.DefaultScene.VisualChildren)
            {
                BuildNodes(nodes, node, Matrix.Identity);
            }

            return new GltfModel
            {
                textures = textures,
                materials = materials,
                meshes = meshes,
                nodes = nodes
            };
        }

        private static void BuildNodes(List<GltfModel.ModelNode> outNodes, SharpGLTF.Schema2.Node node, Matrix parentTransform)
        {
            // build node transform
            Matrix transform = Matrix.CreateScale(node.LocalTransform.Scale.X, node.LocalTransform.Scale.Y, node.LocalTransform.Scale.Z)
                * Matrix.CreateFromQuaternion(new Quaternion(node.LocalTransform.Rotation.X, node.LocalTransform.Rotation.Y, node.LocalTransform.Rotation.Z, node.LocalTransform.Rotation.W))
                * Matrix.CreateTranslation(node.LocalTransform.Translation.X, node.LocalTransform.Translation.Y, node.LocalTransform.Translation.Z);
            
            transform *= parentTransform;

            if (node.Mesh != null)
            {
                outNodes.Add(new GltfModel.ModelNode
                {
                    transform = transform,
                    meshIdx = node.Mesh.LogicalIndex
                });
            }

            foreach (var child in node.VisualChildren)
            {
                BuildNodes(outNodes, child, transform);
            }
        }
    }
}