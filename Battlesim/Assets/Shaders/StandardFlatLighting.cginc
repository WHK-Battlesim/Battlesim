#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"

// Shader uniforms
float4 _Color;

// Vertex data
struct vertData
{
    float4 position : POSITION;
    float3 normal : NORMAL;
};

// Fragment data
struct fragData
{
    float4 position : SV_POSITION;
    float4 color : COLOR0;
};

//
// Vertex stage
//

vertData vert(vertData input)
{
    input.position = mul(unity_ObjectToWorld, input.position);
    input.normal = UnityObjectToWorldNormal(input.normal);
    return input;
}

//
// Geometry stage
//

[maxvertexcount(3)]
void geom(triangle vertData input[3], inout TriangleStream<fragData> outStream)
{
    float3 normal = normalize(cross(input[1].position - input[0].position, input[2].position - input[0].position));
    float4 color = max(0, dot(normal, _WorldSpaceLightPos0.xyz)) * _LightColor0 * _Color + unity_AmbientSky;

    fragData o;
    o.color = color;
    o.position = UnityWorldToClipPos(input[0].position);
    outStream.Append(o);
    o.position = UnityWorldToClipPos(input[1].position);
    outStream.Append(o);
    o.position = UnityWorldToClipPos(input[2].position);
    outStream.Append(o);
    outStream.RestartStrip();
}

//
// Fragment phase
//

float4 frag(fragData input) : SV_Target
{
    return input.color;
}
