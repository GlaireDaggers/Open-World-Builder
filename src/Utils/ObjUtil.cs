using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenWorldBuilder
{
    public static class ObjUtil
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

        public static void ConvertObj(Stream inStream, GraphicsDevice gd, out VertexBuffer vb, out IndexBuffer ib)
        {
            var obj = App.Instance!.objLoader.Load(inStream);

            List<uint> indices = new List<uint>();
            List<MeshVert> vertices = new List<MeshVert>();

            foreach (var group in obj.Groups)
            {
                foreach (var face in group.Faces)
                {
                    if (face.Count == 3)
                    {
                        var faceNormal = CalcFaceNormal(obj, face);

                        for (int i = 0; i < 3; i++)
                        {
                            var vtx = obj.Vertices[face[i].VertexIndex - 1];
                            var normal = obj.Normals.Count > 0 ? obj.Normals[face[i].NormalIndex - 1] : faceNormal;
                            var uv = obj.Textures.Count > 0 ? obj.Textures[face[i].TextureIndex - 1] : new ObjLoader.Loader.Data.VertexData.Texture(0f, 0f);
                            indices.Add((uint)vertices.Count);
                            vertices.Add(new MeshVert
                            {
                                pos = new Vector3(vtx.X, vtx.Y, vtx.Z),
                                normal = new Vector3(normal.X, normal.Y, normal.Z),
                                uv = new Vector2(uv.X, uv.Y),
                                col = Color.White,
                            });
                        }
                    }
                    else if (face.Count > 3)
                    {
                        // triangulate as triangle fan
                        var faceNormal = CalcFaceNormal(obj, face);

                        var vtx0 = obj.Vertices[face[0].VertexIndex - 1];
                        var normal0 = obj.Normals.Count > 0 ? obj.Normals[face[0].NormalIndex - 1] : faceNormal;
                        var uv0 = obj.Textures.Count > 0 ? obj.Textures[face[0].TextureIndex - 1] : new ObjLoader.Loader.Data.VertexData.Texture(0f, 0f);

                        for (int t = 1; t < face.Count - 1; t++)
                        {
                            var vtx1 = obj.Vertices[face[t].VertexIndex - 1];
                            var normal1 = obj.Normals.Count > 0 ? obj.Normals[face[t].NormalIndex - 1] : faceNormal;
                            var uv1 = obj.Textures.Count > 0 ? obj.Textures[face[t].TextureIndex - 1] : new ObjLoader.Loader.Data.VertexData.Texture(0f, 0f);
                            
                            var vtx2 = obj.Vertices[face[t + 1].VertexIndex - 1];
                            var normal2 = obj.Normals.Count > 0 ? obj.Normals[face[t + 1].NormalIndex - 1] : faceNormal;
                            var uv2 = obj.Textures.Count > 0 ? obj.Textures[face[t + 1].TextureIndex - 1] : new ObjLoader.Loader.Data.VertexData.Texture(0f, 0f);

                            indices.Add((uint)vertices.Count);
                            vertices.Add(new MeshVert
                            {
                                pos = new Vector3(vtx0.X, vtx0.Y, vtx0.Z),
                                normal = new Vector3(normal0.X, normal0.Y, normal0.Z),
                                uv = new Vector2(uv0.X, uv0.Y),
                                col = Color.White,
                            });

                            indices.Add((uint)vertices.Count);
                            vertices.Add(new MeshVert
                            {
                                pos = new Vector3(vtx1.X, vtx1.Y, vtx1.Z),
                                normal = new Vector3(normal1.X, normal1.Y, normal1.Z),
                                uv = new Vector2(uv1.X, uv1.Y),
                                col = Color.White,
                            });

                            indices.Add((uint)vertices.Count);
                            vertices.Add(new MeshVert
                            {
                                pos = new Vector3(vtx2.X, vtx2.Y, vtx2.Z),
                                normal = new Vector3(normal2.X, normal2.Y, normal2.Z),
                                uv = new Vector2(uv2.X, uv2.Y),
                                col = Color.White,
                            });
                        }
                    }
                    else
                    {
                        // huh?
                        Console.WriteLine("Skipping invalid face in OBJ");
                    }
                }
            }

            ib = new IndexBuffer(App.Instance!.GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.WriteOnly);
            vb = new VertexBuffer(App.Instance!.GraphicsDevice, MeshVertDeclaration, vertices.Count, BufferUsage.WriteOnly);

            ib.SetData(indices.ToArray());
            vb.SetData(vertices.ToArray());
        }

        private static ObjLoader.Loader.Data.VertexData.Normal CalcFaceNormal(ObjLoader.Loader.Loaders.LoadResult obj, ObjLoader.Loader.Data.Elements.Face face)
        {
            var vtx0 = obj.Vertices[face[0].VertexIndex - 1];
            var vtx1 = obj.Vertices[face[1].VertexIndex - 1];
            var vtx2 = obj.Vertices[face[2].VertexIndex - 1];

            var v0 = new Vector3(vtx0.X, vtx0.Y, vtx0.Z);
            var v1 = new Vector3(vtx1.X, vtx1.Y, vtx1.Z);
            var v2 = new Vector3(vtx2.X, vtx2.Y, vtx2.Z);

            var faceNormal = Vector3.Cross(v1 - v0, v2 - v0) * -1f;
            faceNormal.Normalize();

            return new ObjLoader.Loader.Data.VertexData.Normal(faceNormal.X, faceNormal.Y, faceNormal.Z);
        }
    }
}