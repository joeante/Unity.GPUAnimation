#include "GPUAnimation.hlsl"


float3 TransformObjectToWorld_CustomMatrix(float4x4 objectToWorld, float3 positionOS)
{
    return mul(objectToWorld, float4(positionOS, 1.0)).xyz;
}

//@TODO: Why do i have to create this method myself?

// Transforms normal from object to world space
float3 TransformObjectToWorldNormal_CustomMatrix(float4x4 objectToWorld, float3 normalOS)
{
    // TransformObjectToWorldNormal()
    /// Normal need to be multiply by inverse transpose
    /// return normalize(mul(normalOS, (float3x3)GetWorldToObjectMatrix()));

    //@TODO: validate that this is ok?

    return normalize(mul((float3x3)objectToWorld, normalOS));
}

real3 TransformObjectToWorldDir_CustomMatrix(float4x4 objectToWorld, real3 dirOS)
{
    // Normalize to support uniform scaling
    return normalize(mul((real3x3)objectToWorld, dirOS));
}

float3 TransformObjectToWorld_GPUAnimation_Shadow(float3 positionOS, float2 boneIds, float2 boneInfluences)
{
#if !UNITY_ANY_INSTANCING_ENABLED
    return TransformObjectToWorld(positionOS);
#else

    float4x4 skinMatrix = CalculateSkinMatrix(textureCoordinatesBuffer[unity_InstanceID], boneIds, boneInfluences);
    return TransformObjectToWorld_CustomMatrix(mul(objectToWorldBuffer[unity_InstanceID], skinMatrix), positionOS);
#endif    
}

float3 TransformObjectToWorldNormal_GPUAnimation_Shadow(float3 normalOS, float2 boneIds, float2 boneInfluences)
{
#if !UNITY_ANY_INSTANCING_ENABLED
    return TransformObjectToWorldNormal(normalOS);
#else
    float4x4 skinMatrix = mul(objectToWorldBuffer[unity_InstanceID], CalculateSkinMatrix(textureCoordinatesBuffer[unity_InstanceID], boneIds, boneInfluences));
    return TransformObjectToWorldNormal_CustomMatrix(skinMatrix, normalOS);
    #endif    
}


VertexPositionInputs GetVertexPositionInputs_GPUAnimation(float3 positionOS, float2 boneIds, float2 boneInfluences)
{
#if !UNITY_ANY_INSTANCING_ENABLED
    return GetVertexPositionInputs(positionOS);
#else

    VertexPositionInputs input;
    float4x4 skinMatrix = CalculateSkinMatrix(textureCoordinatesBuffer[unity_InstanceID], boneIds, boneInfluences);
    input.positionWS = TransformObjectToWorld_CustomMatrix(mul(objectToWorldBuffer[unity_InstanceID], skinMatrix), positionOS);

    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);
    
    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;
        
    return input;
#endif
}

VertexNormalInputs GetVertexNormalInputs_GPUAnimation(float3 normalOS, float4 tangentOS, float2 boneIds, float2 boneInfluences)
{
#if !UNITY_ANY_INSTANCING_ENABLED
    return GetVertexNormalInputs(normalOS, tangentOS);
#else
    float4x4 skinMatrix = mul(objectToWorldBuffer[unity_InstanceID], CalculateSkinMatrix(textureCoordinatesBuffer[unity_InstanceID], boneIds, boneInfluences));

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = tangentOS.w * GetOddNegativeScale();

    VertexNormalInputs tbn;
    tbn.normalWS = TransformObjectToWorldNormal_CustomMatrix(skinMatrix, normalOS);
    tbn.tangentWS = TransformObjectToWorldDir_CustomMatrix(skinMatrix, tangentOS.xyz);
    tbn.bitangentWS = cross(tbn.normalWS, tbn.tangentWS) * sign;
    return tbn;
    
    #endif
}

void setup_gpuanimation()
{

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    //unity_ObjectToWorld = objectToWorldBuffer[unity_InstanceID];
    //unity_WorldToObject = unity_ObjectToWorld;

    // Construct an identity matrix
    unity_ObjectToWorld = float4x4(1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);
    unity_WorldToObject = unity_ObjectToWorld;

    unity_WorldToObject._14_24_34 *= -1;
    unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
}