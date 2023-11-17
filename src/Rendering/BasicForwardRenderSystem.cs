using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenWorldBuilder
{
    public class BasicForwardRenderSystem : RenderSystem
    {
        private static string[] terrainAlbedoParameters = new string[]
        {
            "DiffuseTexture0",
            "DiffuseTexture1",
            "DiffuseTexture2",
            "DiffuseTexture3"
        };

        private static string[] terrainTextureScaleParameters = new string[]
        {
            "TextureScale0",
            "TextureScale1",
            "TextureScale2",
            "TextureScale3"
        };


        private Effect _basicLit;
        private Effect _terrainLit;

        private Vector3[] _directionalLightFwd = new Vector3[4];
        private Vector3[] _directionalLightColor = new Vector3[4];

        private Vector4[] _pointLightPosRadius = new Vector4[16];
        private Vector3[] _pointLightColor = new Vector3[16];

        private Vector4[] _spotLightPosRadius = new Vector4[8];
        private Vector4[] _spotLightFwdAngle1 = new Vector4[8];
        private Vector4[] _spotLightColorAngle2 = new Vector4[8];

        private List<RenderLight> _tmpDirectional = new List<RenderLight>();
        private List<RenderLight> _tmpPointLights = new List<RenderLight>();
        private List<RenderLight> _tmpSpotLights = new List<RenderLight>();

        private Vector3 _cachedMeshPos = Vector3.Zero;

        public BasicForwardRenderSystem()
        {
            _basicLit = App.Instance!.Content.Load<Effect>("content/shader/BasicLit.fxo");
            _terrainLit = App.Instance!.Content.Load<Effect>("content/shader/TerrainLit.fxo");
        }

        protected override void OnDraw(Matrix view, Matrix projection, List<RenderLight> lights, List<RenderTerrainPatch> patchQueue, List<RenderMesh> opaqueQueue, List<RenderMesh> transparentQueue)
        {
            _tmpDirectional.Clear();
            _tmpPointLights.Clear();
            _tmpSpotLights.Clear();

            // gather lights
            foreach (var l in lights)
            {
                switch (l.lightType)
                {
                    case LightType.Directional: _tmpDirectional.Add(l); break;
                    case LightType.Point: _tmpPointLights.Add(l); break;
                    case LightType.Spot: _tmpSpotLights.Add(l); break;
                }
            }

            // assign up to 4 directional lights
            int dirLightCount = _tmpDirectional.Count;
            if (dirLightCount > 4) dirLightCount = 4;

            for (int i = 0; i < dirLightCount; i++)
            {
                _directionalLightFwd[i] = _tmpDirectional[i].forward;
                _directionalLightColor[i] = _tmpDirectional[i].color.ToVector3() * _tmpDirectional[i].intensity;
            }

            _basicLit.Parameters["AmbientLightColor"].SetValue(App.Instance!.ActiveLevel.root.ambientColor.ToVector3() * App.Instance!.ActiveLevel.root.ambientIntensity);
            _basicLit.Parameters["DirectionalLightCount"].SetValue(dirLightCount);
            _basicLit.Parameters["DirectionalLightFwd"].SetValue(_directionalLightFwd);
            _basicLit.Parameters["DirectionalLightCol"].SetValue(_directionalLightColor);

            _basicLit.Parameters["ViewProjection"].SetValue(view * projection);

            _terrainLit.Parameters["AmbientLightColor"].SetValue(App.Instance!.ActiveLevel.root.ambientColor.ToVector3() * App.Instance!.ActiveLevel.root.ambientIntensity);
            _terrainLit.Parameters["DirectionalLightCount"].SetValue(dirLightCount);
            _terrainLit.Parameters["DirectionalLightFwd"].SetValue(_directionalLightFwd);
            _terrainLit.Parameters["DirectionalLightCol"].SetValue(_directionalLightColor);

            _terrainLit.Parameters["ViewProjection"].SetValue(view * projection);

            App.Instance!.GraphicsDevice.BlendState = BlendState.Opaque;
            App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            DrawQueue(patchQueue);

            App.Instance!.GraphicsDevice.BlendState = BlendState.Opaque;
            App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            DrawQueue(opaqueQueue);

            App.Instance!.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
            App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

            DrawQueue(transparentQueue);
        }

        private void DrawQueue(List<RenderTerrainPatch> queue)
        {
            foreach (var renderPatch in queue)
            {
                _cachedMeshPos = renderPatch.transform.Translation;

                // sort point+spot lights by distance to mesh center
                _tmpPointLights.Sort(SortLightsByDistance);
                _tmpSpotLights.Sort(SortLightsByDistance);
                
                // assign point lights
                int pointLightCount = _tmpPointLights.Count;
                if (pointLightCount > 16) pointLightCount = 16;

                for (int i = 0; i < pointLightCount; i++)
                {
                    _pointLightPosRadius[i] = new Vector4(_tmpPointLights[i].position, _tmpPointLights[i].radius);
                    _pointLightColor[i] = _tmpPointLights[i].color.ToVector3() * _tmpPointLights[i].intensity;
                }

                _terrainLit.Parameters["PointLightCount"].SetValue(pointLightCount);
                _terrainLit.Parameters["PointLightPosRadius"].SetValue(_pointLightPosRadius);
                _terrainLit.Parameters["PointLightCol"].SetValue(_pointLightColor);

                // assign spot lights
                int spotLightCount = _tmpSpotLights.Count;
                if (spotLightCount > 8) spotLightCount = 8;

                for (int i = 0; i < spotLightCount; i++)
                {
                    float angle1 = MathF.Cos(MathHelper.ToRadians(_tmpSpotLights[i].innerConeAngle));
                    float angle2 = MathF.Cos(MathHelper.ToRadians(_tmpSpotLights[i].outerConeAngle));

                    _spotLightPosRadius[i] = new Vector4(_tmpSpotLights[i].position, _tmpSpotLights[i].radius);
                    _spotLightFwdAngle1[i] = new Vector4(_tmpSpotLights[i].forward, angle1);
                    _spotLightColorAngle2[i] = new Vector4(_tmpSpotLights[i].color.ToVector3() * _tmpSpotLights[i].intensity, angle2);
                }

                _terrainLit.Parameters["SpotLightCount"].SetValue(spotLightCount);
                _terrainLit.Parameters["SpotLightPosRadius"].SetValue(_spotLightPosRadius);
                _terrainLit.Parameters["SpotLightFwdAngle1"].SetValue(_spotLightFwdAngle1);
                _terrainLit.Parameters["SpotLightColAngle2"].SetValue(_spotLightColorAngle2);

                _terrainLit.Parameters["World"]?.SetValue(renderPatch.transform);

                App.Instance!.GraphicsDevice.SetVertexBuffer(renderPatch.meshPart.vb);
                App.Instance!.GraphicsDevice.Indices = renderPatch.meshPart.ib;

                _terrainLit.Parameters["HeightmapTexture"]?.SetValue(renderPatch.material.heightmap);
                App.Instance!.GraphicsDevice.VertexTextures[0] = renderPatch.material.heightmap;
                App.Instance!.GraphicsDevice.VertexSamplerStates[0] = SamplerState.LinearClamp;

                _terrainLit.Parameters["UVOffsetScale"]?.SetValue(new Vector4(
                    renderPatch.material.uvOffset.X,
                    renderPatch.material.uvOffset.Y,
                    renderPatch.material.uvScale.X,
                    renderPatch.material.uvScale.Y
                ));

                _terrainLit.Parameters["TerrainSize"]?.SetValue(renderPatch.material.terrainSize);
                _terrainLit.Parameters["TerrainHeight"]?.SetValue(renderPatch.material.terrainHeight);
                _terrainLit.Parameters["HeightmapTexelSize"]?.SetValue(1.0f / renderPatch.material.heightmap.Width);

                for (int i = 0; i < renderPatch.material.layers.Length; i += 4)
                {
                    _terrainLit.Parameters["SplatmapTexture"]?.SetValue(renderPatch.material.splatmaps[i / 4]);
                    App.Instance!.GraphicsDevice.Textures[0] = renderPatch.material.splatmaps[i / 4];

                    int layersInPass = renderPatch.material.layers.Length - i;
                    if (layersInPass > 4) layersInPass = 4;

                    if (i == 0)
                    {
                        App.Instance!.GraphicsDevice.BlendState = BlendState.Opaque;
                        App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    }
                    else
                    {
                        App.Instance!.GraphicsDevice.BlendState = BlendState.Additive;
                        App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                    }

                    for (int j = 0; j < layersInPass; j++)
                    {
                        _terrainLit.Parameters[terrainTextureScaleParameters[j]]?.SetValue(renderPatch.material.layers[i + j].scale);
                        _terrainLit.Parameters[terrainAlbedoParameters[j]]?.SetValue(renderPatch.material.layers[i + j].albedoTexture);
                        App.Instance!.GraphicsDevice.Textures[1 + j] = renderPatch.material.layers[i + j].albedoTexture;
                    }

                    _terrainLit.CurrentTechnique.Passes[0].Apply();
                    App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, renderPatch.meshPart.vb.VertexCount, 0, renderPatch.meshPart.ib.IndexCount / 3);
                }
            }
        }

        private void DrawQueue(List<RenderMesh> queue)
        {
            foreach (var renderMesh in queue)
            {
                _cachedMeshPos = renderMesh.transform.Translation;

                // sort point+spot lights by distance to mesh center
                _tmpPointLights.Sort(SortLightsByDistance);
                _tmpSpotLights.Sort(SortLightsByDistance);
                
                // assign point lights
                int pointLightCount = _tmpPointLights.Count;
                if (pointLightCount > 16) pointLightCount = 16;

                for (int i = 0; i < pointLightCount; i++)
                {
                    _pointLightPosRadius[i] = new Vector4(_tmpPointLights[i].position, _tmpPointLights[i].radius);
                    _pointLightColor[i] = _tmpPointLights[i].color.ToVector3() * _tmpPointLights[i].intensity;
                }

                _basicLit.Parameters["PointLightCount"].SetValue(pointLightCount);
                _basicLit.Parameters["PointLightPosRadius"].SetValue(_pointLightPosRadius);
                _basicLit.Parameters["PointLightCol"].SetValue(_pointLightColor);

                // assign spot lights
                int spotLightCount = _tmpSpotLights.Count;
                if (spotLightCount > 8) spotLightCount = 8;

                for (int i = 0; i < spotLightCount; i++)
                {
                    float angle1 = MathF.Cos(MathHelper.ToRadians(_tmpSpotLights[i].innerConeAngle));
                    float angle2 = MathF.Cos(MathHelper.ToRadians(_tmpSpotLights[i].outerConeAngle));

                    _spotLightPosRadius[i] = new Vector4(_tmpSpotLights[i].position, _tmpSpotLights[i].radius);
                    _spotLightFwdAngle1[i] = new Vector4(_tmpSpotLights[i].forward, angle1);
                    _spotLightColorAngle2[i] = new Vector4(_tmpSpotLights[i].color.ToVector3() * _tmpSpotLights[i].intensity, angle2);
                }

                _basicLit.Parameters["SpotLightCount"].SetValue(spotLightCount);
                _basicLit.Parameters["SpotLightPosRadius"].SetValue(_spotLightPosRadius);
                _basicLit.Parameters["SpotLightFwdAngle1"].SetValue(_spotLightFwdAngle1);
                _basicLit.Parameters["SpotLightColAngle2"].SetValue(_spotLightColorAngle2);

                if (renderMesh.material.albedoTexture == null)
                {
                    _basicLit.CurrentTechnique = _basicLit.Techniques["NoTexture"];
                }
                else if (renderMesh.material.alphaMode == RenderMaterialAlphaMode.Mask)
                {
                    _basicLit.CurrentTechnique = _basicLit.Techniques["Texture_Mask"];
                }
                else
                {
                    _basicLit.CurrentTechnique = _basicLit.Techniques["Texture"];
                }

                _basicLit.Parameters["World"]?.SetValue(renderMesh.transform);
                _basicLit.Parameters["DiffuseColor"]?.SetValue(renderMesh.material.albedo.ToVector4());
                _basicLit.Parameters["DiffuseTexture"]?.SetValue(renderMesh.material.albedoTexture);
                _basicLit.Parameters["AlphaCutoff"]?.SetValue(renderMesh.material.alphaCutoff);

                App.Instance!.GraphicsDevice.Textures[0] = renderMesh.material.albedoTexture;

                App.Instance!.GraphicsDevice.SetVertexBuffer(renderMesh.meshPart.vb);
                App.Instance!.GraphicsDevice.Indices = renderMesh.meshPart.ib;

                _basicLit.CurrentTechnique.Passes[0].Apply();

                App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, renderMesh.meshPart.vb.VertexCount, 0, renderMesh.meshPart.ib.IndexCount / 3);
            }
        }

        private int SortLightsByDistance(RenderLight a, RenderLight b)
        {
            float distA = Vector3.DistanceSquared(a.position, _cachedMeshPos);
            float distB = Vector3.DistanceSquared(b.position, _cachedMeshPos);

            return distA.CompareTo(distB);
        }
    }
}