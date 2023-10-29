using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenWorldBuilder
{
    public enum RenderMaterialAlphaMode
    {
        Opaque,
        Mask,
        Blend,
    }

    public struct RenderMaterial
    {
        public RenderMaterialAlphaMode alphaMode;
        public float alphaCutoff;
        public Color albedo;
        public float roughness;
        public float metallic;
        public Texture2D albedoTexture;
        public Texture2D ormTexture;
        public Texture2D normalTexture;
    }

    /// <summary>
    /// A render system is responsible for drawing visual nodes
    /// </summary>
    public class RenderSystem
    {
        public struct RenderMesh
        {
            public Matrix transform;
            public GltfMeshPart meshPart;
            public RenderMaterial material;
        }

        public struct RenderLight
        {
            public Vector3 position;
            public Vector3 forward;
            public LightType lightType;
            public Color color;
            public float intensity;
            public float radius;
            public float innerConeAngle;
            public float outerConeAngle;
        }

        private List<RenderMesh> _opaqueQueue = new List<RenderMesh>();
        private List<RenderMesh> _transparentQueue = new List<RenderMesh>();
        private List<RenderLight> _lights = new List<RenderLight>();
        
        private Matrix _cachedView;

        public void Draw(SceneRootNode scene, Matrix view, Matrix projection)
        {
            _opaqueQueue.Clear();
            _transparentQueue.Clear();
            _lights.Clear();

            GatherLists(scene);

            // sort queues
            _cachedView = view;
            _opaqueQueue.Sort(SortMeshFrontToBack);
            _transparentQueue.Sort(SortMeshBackToFront);

            // draw implementation
            OnDraw(view, projection, _lights, _opaqueQueue, _transparentQueue);
        }

        public void SubmitMesh(RenderMesh renderMesh)
        {
            if (renderMesh.material.alphaMode == RenderMaterialAlphaMode.Blend)
            {
                _transparentQueue.Add(renderMesh);
            }
            else
            {
                _opaqueQueue.Add(renderMesh);
            }
        }

        public void SubmitLight(RenderLight light)
        {
            _lights.Add(light);
        }

        private int SortMeshFrontToBack(RenderMesh a, RenderMesh b)
        {
            Vector3 posA = a.transform.Translation;
            Vector3 posB = b.transform.Translation;

            posA = Vector3.Transform(posA, _cachedView);
            posB = Vector3.Transform(posB, _cachedView);

            return posB.Z.CompareTo(posA.Z);
        }

        private int SortMeshBackToFront(RenderMesh a, RenderMesh b)
        {
            Vector3 posA = a.transform.Translation;
            Vector3 posB = b.transform.Translation;

            posA = Vector3.Transform(posA, _cachedView);
            posB = Vector3.Transform(posB, _cachedView);

            return posA.Z.CompareTo(posB.Z);
        }

        private void GatherLists(Node node)
        {
            node.DrawScene(this);

            foreach (var child in node.Children)
            {
                GatherLists(child);
            }
        }

        protected virtual void OnDraw(Matrix view, Matrix projection, List<RenderLight> lights, List<RenderMesh> opaqueQueue, List<RenderMesh> transparentQueue)
        {
        }
    }
}