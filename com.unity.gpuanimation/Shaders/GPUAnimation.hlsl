#ifndef GPUANIMATION_HLSL_INCLUDED
#define GPUANIMATION_HLSL_INCLUDED

StructuredBuffer<float4x4> objectToWorldBuffer;
StructuredBuffer<float4>   textureCoordinatesBuffer;

SamplerState a_point_clamp_sampler;

//@TODO: Use vertex skinning node
#define UNITY_VERTEX_INPUT_GPUANIMATION float2 boneIds  : TEXCOORD1; float2 boneInfluences : TEXCOORD2; 



//@TODO: use 4x3 matrix
inline float4x4 CreateMatrix(float texturePosition, float boneId,
    TEXTURE2D(AnimationTexture0),
    TEXTURE2D(AnimationTexture1),
    TEXTURE2D(AnimationTexture2))
{
	float4 row0 = SAMPLE_TEXTURE2D_LOD(AnimationTexture0, a_point_clamp_sampler, float2(texturePosition, boneId), 0);
	float4 row1 = SAMPLE_TEXTURE2D_LOD(AnimationTexture1, a_point_clamp_sampler, float2(texturePosition, boneId), 0);
	float4 row2 = SAMPLE_TEXTURE2D_LOD(AnimationTexture2, a_point_clamp_sampler, float2(texturePosition, boneId), 0);

	float4x4 reconstructedMatrix = float4x4(row0, row1, row2, float4(0, 0, 0, 1));

	return reconstructedMatrix;
}

inline float4x4 CalculateSkinMatrix(float3 animationTextureCoords, float2 boneIds, float2 boneInfluences,
    TEXTURE2D(AnimationTexture0),
    TEXTURE2D(AnimationTexture1),
    TEXTURE2D(AnimationTexture2))
{
	// We interpolate between two matrices
	float4x4 frame0_BoneMatrix0 = CreateMatrix(animationTextureCoords.x, boneIds.x,
        AnimationTexture0,
        AnimationTexture1,
        AnimationTexture2);
	float4x4 frame0_BoneMatrix1 = CreateMatrix(animationTextureCoords.y, boneIds.x,
        AnimationTexture0,
        AnimationTexture1,
        AnimationTexture2);
	float4x4 frame0_BoneMatrix = frame0_BoneMatrix0 * (1 - animationTextureCoords.z) + frame0_BoneMatrix1 * animationTextureCoords.z;

	float4x4 frame1_BoneMatrix0 = CreateMatrix(animationTextureCoords.x, boneIds.y,
        AnimationTexture0,
        AnimationTexture1,
        AnimationTexture2);
	float4x4 frame1_BoneMatrix1= CreateMatrix(animationTextureCoords.y, boneIds.y,
        AnimationTexture0,
        AnimationTexture1,
        AnimationTexture2);
	float4x4 frame1_BoneMatrix = frame1_BoneMatrix0 * (1 - animationTextureCoords.z) + frame1_BoneMatrix1 * animationTextureCoords.z;

	return frame0_BoneMatrix * boneInfluences.x + frame1_BoneMatrix * boneInfluences.y;
}

inline void ApplySkinning_float(
    float3 Position, float3 Normal, float3 Tangent, float2 BoneIndex, float2 BoneWeight, float3 AnimationTextureCoords,
    TEXTURE2D(AnimationTexture0),
    TEXTURE2D(AnimationTexture1),
    TEXTURE2D(AnimationTexture2),
    out float3 OutPosition, out float3 OutNormal, out float3 OutTangent)
{
    float4x4 skinMatrix = CalculateSkinMatrix(AnimationTextureCoords, BoneIndex, BoneWeight,
        AnimationTexture0,
        AnimationTexture1,
        AnimationTexture2);

    OutPosition = mul(skinMatrix, float4(Position, 1));
    OutNormal   = mul(skinMatrix, float4(Normal, 0));
    OutTangent  = mul(skinMatrix, float4(Tangent, 0));
}
#endif