using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Unity.GPUAnimation
{
    [MaterialProperty("AnimationTextureCoordinate", MaterialPropertyFormat.Float4)]
    struct AnimationTextureCoordinate : IComponentData
    {
        public float4 Coordinate;
    }
    
	public struct GPUAnimationState : IComponentData
	{
		public float Time;
		public int   AnimationClipIndex;
		
		public BlobAssetReference<BakedAnimationClipSet> AnimationClipSet;
	}
	
	public struct BakedAnimationClipSet
	{
		public BlobArray<BakedAnimationClip> Clips;
	}

	public struct BakedAnimationClip
	{
		internal float TextureOffset;
		internal float TextureRange;
		internal float OnePixelOffset;
		internal float TextureWidth;
		internal float OneOverTextureWidth;
		internal float OneOverPixelOffset;

		public float AnimationLength;
		public bool  Looping;

		public BakedAnimationClip(AnimationTextures animTextures, KeyframeTextureBaker.AnimationClipData clipData)
		{
			float onePixel = 1f / animTextures.Animation0.width;
			float start = (float)clipData.PixelStart / animTextures.Animation0.width;
			float end = (float)clipData.PixelEnd / animTextures.Animation0.width;

			TextureOffset = start;
			TextureRange = end - start;
			OnePixelOffset = onePixel;
			TextureWidth = animTextures.Animation0.width;
			OneOverTextureWidth = 1.0F / TextureWidth;
			OneOverPixelOffset = 1.0F / OnePixelOffset;
			
			AnimationLength = clipData.Clip.length;
			Looping = clipData.Clip.wrapMode == WrapMode.Loop;
		}
		
		public float3 ComputeCoordinate(float normalizedTime)
		{
			float texturePosition = normalizedTime * TextureRange + TextureOffset;
			float lowerPixelFloor = math.floor(texturePosition * TextureWidth);

			float lowerPixelCenter = lowerPixelFloor * OneOverTextureWidth;
			float upperPixelCenter = lowerPixelCenter + OnePixelOffset;
			float lerpFactor = (texturePosition - lowerPixelCenter) * OneOverPixelOffset;

			return  new float3(lowerPixelCenter, upperPixelCenter, lerpFactor);
		}
		
		public float ComputeNormalizedTime(float time)
		{
			if (Looping)
				return Mathf.Repeat(time, AnimationLength) / AnimationLength;
			else
				return math.saturate(time / AnimationLength);
		}

	}
}