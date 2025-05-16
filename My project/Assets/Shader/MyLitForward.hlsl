// Pull in URP lib functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


struct Attributes {
    float3 position : POSITIONT;
};

struct VertexPositionsInputs {
    float3 positionWS; // World space position";
    float3 positionVS; // View space position
    float4 positionCS; // Homogeneous clip space position
    float3 positionNDC; // Homogeneous normalized device coordinates
};

void Vertex(Attributes input)
{
    VertexPositionsInputs posnInputs = GetVertexPositionInputs(input.position);
    
    float3 positionClipSpace = posnInputs.positionCS;
}