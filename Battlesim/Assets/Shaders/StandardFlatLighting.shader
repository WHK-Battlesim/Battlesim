Shader "Standard (Flat Lighting)"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "StandardFlatLighting.cginc"
            ENDCG
        }
    }
	Fallback "Standard"
}