sampler2D _AnimationTexture0;
sampler2D _AnimationTexture1;
sampler2D _AnimationTexture2;

StructuredBuffer<float4x4> objectToWorldBuffer;
StructuredBuffer<float3>   textureCoordinatesBuffer;

//@TODO: Use vertex skinning node
#define UNITY_VERTEX_INPUT_GPUANIMATION float2 boneIds  : TEXCOORD1; float2 boneInfluences : TEXCOORD2; 



//@TODO: use 4x3 matrix
inline float4x4 CreateMatrix(float texturePosition, float boneId)
{
	float4 row0 = tex2Dlod(_AnimationTexture0, float4(texturePosition, boneId, 0, 0));
	float4 row1 = tex2Dlod(_AnimationTexture1, float4(texturePosition, boneId, 0, 0));
	float4 row2 = tex2Dlod(_AnimationTexture2, float4(texturePosition, boneId, 0, 0));

	float4x4 reconstructedMatrix = float4x4(row0, row1, row2, float4(0, 0, 0, 1));

	return reconstructedMatrix;
}

inline float4x4 CalculateSkinMatrix(float3 animationTextureCoords, float2 boneIds, float2 boneInfluences)
{
	// We interpolate between two matrices
	float4x4 frame0_BoneMatrix0 = CreateMatrix(animationTextureCoords.x, boneIds.x);
	float4x4 frame0_BoneMatrix1 = CreateMatrix(animationTextureCoords.y, boneIds.x);
	float4x4 frame0_BoneMatrix = frame0_BoneMatrix0 * (1 - animationTextureCoords.z) + frame0_BoneMatrix1 * animationTextureCoords.z;

	float4x4 frame1_BoneMatrix0 = CreateMatrix(animationTextureCoords.x, boneIds.y);
	float4x4 frame1_BoneMatrix1= CreateMatrix(animationTextureCoords.y, boneIds.y);
	float4x4 frame1_BoneMatrix = frame1_BoneMatrix0 * (1 - animationTextureCoords.z) + frame1_BoneMatrix1 * animationTextureCoords.z;

	return frame0_BoneMatrix * boneInfluences.x + frame1_BoneMatrix * boneInfluences.y;
}

