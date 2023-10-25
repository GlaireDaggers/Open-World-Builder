float4x4 ViewProjection;
float4x4 World;

float4 DiffuseColor;
sampler2D DiffuseTexture;

float AlphaCutoff;

int DirectionalLightCount;
int PointLightCount;
int SpotLightCount;

float3 DirectionalLightFwd[4];
float3 DirectionalLightCol[4];

float4 PointLightPosRadius[16];
float3 PointLightCol[16];

float4 SpotLightPosRadius[8];
float4 SpotLightFwdAngle1[8];
float4 SpotLightColAngle2[8];

struct VertexInput {
    float3 position: POSITION0;
    float3 normal: NORMAL0;
    float4 color: COLOR0;
    float2 texcoord: TEXCOORD0;
};

struct PixelInput {
    float4 position: SV_Position0;
    float3 normal: NORMAL0;
    float4 color: COLOR0;
    float2 texcoord: TEXCOORD0;
    float3 wpos: TEXCOORD1;
};

PixelInput BasicLitVS(VertexInput v) {
    PixelInput o;

    o.position = mul(mul(float4(v.position, 1.0), World), ViewProjection);
    o.wpos = mul(float4(v.position, 1.0), World).xyz;
    o.color = v.color * DiffuseColor;
    o.normal = normalize(mul(float4(v.normal, 0.0), World).xyz);
    o.texcoord = v.texcoord;

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

float3 BasicLitPS_Core(float3 diffuse, float3 wpos, float3 normal) {
    float3 col = float3(0, 0, 0);
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

float4 BasicLitPS_NoTexture(PixelInput p) : SV_TARGET {
    float3 col = BasicLitPS_Core(p.color.xyz, p.wpos, p.normal);
    return float4(col, p.color.w);
}

float4 BasicLitPS_Texture(PixelInput p) : SV_TARGET {
    float4 tex = tex2D(DiffuseTexture, p.texcoord);
    float4 diffuseCol = tex * p.color;
    float3 col = BasicLitPS_Core(diffuseCol.xyz, p.wpos, p.normal);
    return float4(col, diffuseCol.w);
}

float4 BasicLitPS_Texture_Mask(PixelInput p) : SV_TARGET {
    float4 tex = tex2D(DiffuseTexture, p.texcoord);

    if (tex.a < AlphaCutoff) {
        discard;
    }

    float4 diffuseCol = tex * p.color;
    float3 col = BasicLitPS_Core(diffuseCol.xyz, p.wpos, p.normal);
    return float4(col, diffuseCol.w);
}

technique NoTexture {
    pass {
        VertexShader = compile vs_3_0 BasicLitVS();
        PixelShader = compile ps_3_0 BasicLitPS_NoTexture();
    }
}

technique Texture {
    pass {
        VertexShader = compile vs_3_0 BasicLitVS();
        PixelShader = compile ps_3_0 BasicLitPS_Texture();
    }
}

technique Texture_Mask {
    pass {
        VertexShader = compile vs_3_0 BasicLitVS();
        PixelShader = compile ps_3_0 BasicLitPS_Texture_Mask();
    }
}