float4x4 ViewProjection;
float4x4 World;

sampler2D HeightmapTexture : register(s0);
sampler2D SplatmapTexture : register(s0);

sampler2D DiffuseTexture0 : register(s1);
sampler2D DiffuseTexture1 : register(s2);
sampler2D DiffuseTexture2 : register(s3);
sampler2D DiffuseTexture3 : register(s4);

float4 UVOffsetScale;

float TextureScale0;
float TextureScale1;
float TextureScale2;
float TextureScale3;

float TerrainSize;
float TerrainHeight;
float HeightmapTexelSize;

int DirectionalLightCount;
int PointLightCount;
int SpotLightCount;

float3 AmbientLightColor;

float3 DirectionalLightFwd[4];
float3 DirectionalLightCol[4];

float4 PointLightPosRadius[16];
float3 PointLightCol[16];

float4 SpotLightPosRadius[8];
float4 SpotLightFwdAngle1[8];
float4 SpotLightColAngle2[8];

struct VertexInput {
    float3 position: POSITION0;
    float2 texcoord: TEXCOORD0;
};

struct PixelInput {
    float4 position: SV_Position0;
    float3 normal: NORMAL0;
    float2 texcoord: TEXCOORD0;
    float2 texcoord0: TEXCOORD1;
    float2 texcoord1: TEXCOORD2;
    float2 texcoord2: TEXCOORD3;
    float2 texcoord3: TEXCOORD4;
    float3 wpos: TEXCOORD5;
};

float3 SampleNormalFilter(float2 uv)
{
	float4 h;
	h.x = tex2Dlod(HeightmapTexture, float4(uv + (HeightmapTexelSize * float2(0, -1)), 0, 0)).r * TerrainHeight;
	h.y = tex2Dlod(HeightmapTexture, float4(uv + (HeightmapTexelSize * float2(-1, 0)), 0, 0)).r * TerrainHeight;
	h.z = tex2Dlod(HeightmapTexture, float4(uv + (HeightmapTexelSize * float2(1, 0)), 0, 0)).r * TerrainHeight;
	h.w = tex2Dlod(HeightmapTexture, float4(uv + (HeightmapTexelSize * float2(0, 1)), 0, 0)).r * TerrainHeight;
	
	float3 n;
	n.z = h.x - h.w;
	n.x = h.y - h.z;
	n.y = 2 * HeightmapTexelSize * TerrainSize / UVOffsetScale.z;
	
	return normalize(n);
}

PixelInput TerrainLitVS(VertexInput v) {
    PixelInput o;
    
    float2 uv = (v.texcoord * UVOffsetScale.zw) + UVOffsetScale.xy;
    float height = tex2Dlod(HeightmapTexture, float4(uv, 0, 0)).r;
    
    float3 pos = v.position + float3(0, height * TerrainHeight, 0);
    float3 normal = SampleNormalFilter(uv);
    
    pos.x *= TerrainSize;
    pos.z *= TerrainSize;

    o.position = mul(mul(float4(pos, 1.0), World), ViewProjection);
    o.wpos = mul(float4(pos, 1.0), World).xyz;
    o.normal = normalize(mul(float4(normal, 0.0), World).xyz);
    o.texcoord = uv;
    o.texcoord0 = uv * TextureScale0;
    o.texcoord1 = uv * TextureScale1;
    o.texcoord2 = uv * TextureScale2;
    o.texcoord3 = uv * TextureScale3;

    return o;
}

float3 CalcBXDF(float3 lightDir, float3 lightCol, float lightAtten, float3 diffuse, float3 normal) {
    float nl = saturate(dot(lightDir, normal));
    return nl * lightAtten * lightCol * diffuse;
}

float3 CalcDirLight(float3 lightDir, float3 lightCol, float3 diffuse, float3 normal) {
    return CalcBXDF(-lightDir, lightCol, 1.0, diffuse, normal);
}

float3 CalcPointLight(float4 lightPosRadius, float3 lightCol, float3 diffuse, float3 wpos, float3 normal) {
    float3 lightToSurf = lightPosRadius.xyz - wpos;
    float dist = length(lightToSurf);
    float3 l = lightToSurf / dist;
    
    // adapted from https://lisyarus.github.io/blog/graphics/2022/07/30/point-light-attenuation.html
    float s = dist / lightPosRadius.w;
    float s2 = s * s;
    float one_minus_s2 = 1.0 - s2;
    float atten = (one_minus_s2 * one_minus_s2) / (1 + s2);
    atten *= step(dist, lightPosRadius.w);

    if (dist > lightPosRadius.w) {
        atten = 0.0;
    }

    return CalcBXDF(l, lightCol, atten, diffuse, normal);
}

float3 CalcSpotLight(float4 lightPosRadius, float4 lightDirAngle1, float4 lightColAngle2, float3 diffuse, float3 wpos, float3 normal) {
    float3 lightToSurf = lightPosRadius.xyz - wpos;
    float dist = length(lightToSurf);
    float3 l = lightToSurf / dist;

    float spotAngle = -dot(l, lightDirAngle1.xyz);
    float spotAtten = 1.0 - smoothstep(lightDirAngle1.w, lightColAngle2.w, spotAngle);

    // adapted from https://lisyarus.github.io/blog/graphics/2022/07/30/point-light-attenuation.html
    float s = dist / lightPosRadius.w;
    float s2 = s * s;
    float one_minus_s2 = 1.0 - s2;
    float atten = (one_minus_s2 * one_minus_s2) / (1 + s2);
    atten *= step(dist, lightPosRadius.w);

    return CalcBXDF(l, lightColAngle2.xyz, atten * spotAtten, diffuse, normal);
}

float3 TerrainLitPS_Core(float3 diffuse, float3 wpos, float3 normal) {
    float3 col = diffuse * AmbientLightColor;
    int i = 0;

    for (i = 0; i < DirectionalLightCount; i++) {
        col += CalcDirLight(DirectionalLightFwd[i], DirectionalLightCol[i], diffuse, normal);
    }

    for (i = 0; i < PointLightCount; i++) {
        col += CalcPointLight(PointLightPosRadius[i], PointLightCol[i], diffuse, wpos, normal);
    }

    for (i = 0; i < SpotLightCount; i++) {
        col += CalcSpotLight(SpotLightPosRadius[i], SpotLightFwdAngle1[i], SpotLightColAngle2[i], diffuse, wpos, normal);
    }

    return col;
}

float4 TerrainLitPS(PixelInput p) : SV_TARGET {
	float4 splat = tex2D(SplatmapTexture, p.texcoord);
    float4 tex0 = tex2D(DiffuseTexture0, p.texcoord0);
    float4 tex1 = tex2D(DiffuseTexture1, p.texcoord1);
    float4 tex2 = tex2D(DiffuseTexture2, p.texcoord2);
    float4 tex3 = tex2D(DiffuseTexture3, p.texcoord3);
    
    float4 diffuseCol = (tex0 * splat.r) + (tex1 * splat.g) + (tex2 * splat.b) + (tex3 * splat.a);
    
    float3 col = TerrainLitPS_Core(diffuseCol.xyz, p.wpos, p.normal);
    return float4(col, 1.0);
}

technique Main {
    pass {
        VertexShader = compile vs_3_0 TerrainLitVS();
        PixelShader = compile ps_3_0 TerrainLitPS();
    }
}
