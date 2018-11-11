#include "UnityCG.cginc"
#include "UnityLightingCommon.cginc"

// Shader uniforms
float4 _Color;

// Vertex data
struct vertData
{
    float4 position : POSITION;
	float4 normal : NORMAL;
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

fragData vert(vertData input)
{
	fragData output;
    float4 pos = mul(unity_ObjectToWorld, input.position);
	output.position = UnityWorldToClipPos(pos);
    output.color = max(0, dot(input.normal, _WorldSpaceLightPos0.xyz)) * _LightColor0 * _Color + unity_AmbientSky;
    return output;
}

//
// Fragment phase
//

float4 frag(fragData input) : SV_Target
{
    return input.color;
}
