using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace GPUAnimPackage
{
	/*
	struct AnimationState : IComponentData
	{
		public float NormalizedTime;
		public int   AnimationClipIndex;
	}
	*/

	public class ConvertCharacter : MonoBehaviour
	{
		public Material Material;
		public GameObject CharacterRig;
		public AnimationClip[] Clips;
		private InstancedSkinningDrawer drawer;
		private KeyframeTextureBaker.BakedData baked;


		NativeArray<AnimationClipDataBaked> ClipDataBaked;
			
		public int animationIndex;
		
		public struct AnimationClipDataBaked
		{
			public float TextureOffset;
			public float TextureRange;
			public float OnePixelOffset;
			public int TextureWidth;

			public float AnimationLength;
			public bool Looping;

			public float3 ComputeCoordinate(float normalizedTime)
			{
				float texturePosition = normalizedTime * TextureRange + TextureOffset;
				int lowerPixelInt = (int)math.floor(texturePosition * TextureWidth);

				float lowerPixelCenter = (lowerPixelInt * 1.0f) / TextureWidth;
				float upperPixelCenter = lowerPixelCenter + OnePixelOffset;
				float lerpFactor = (texturePosition - lowerPixelCenter) / OnePixelOffset;
				float3 texturePositionData = new float3(lowerPixelCenter, upperPixelCenter, lerpFactor);
				
				return texturePositionData;
			}
		}
		
		private void GetTextureRangeAndOffset(KeyframeTextureBaker.BakedData bakedData, KeyframeTextureBaker.AnimationClipData clipData, out float range, out float offset, out float onePixelOffset, out int textureWidth)
		{
			float onePixel = 1f / bakedData.Texture0.width;
			float start = (float)clipData.PixelStart / bakedData.Texture0.width + onePixel * 0.5f;
			float end = (float)clipData.PixelEnd / bakedData.Texture0.width + onePixel * 0.5f;
			onePixelOffset = onePixel;
			textureWidth = bakedData.Texture0.width;
			range = end - start;
			offset = start;
		}
		
		void OnEnable ()
		{
			var renderer = CharacterRig.GetComponentInChildren<SkinnedMeshRenderer>();

			var lod = new LodData
			{
				Lod1Mesh = renderer.sharedMesh,
				Lod2Mesh = renderer.sharedMesh,
				Lod3Mesh = renderer.sharedMesh,
				Lod1Distance = 0,
				Lod2Distance = 100,
				Lod3Distance = 10000,
			};

			baked = KeyframeTextureBaker.BakeClips(CharacterRig, Clips, lod);

			ClipDataBaked = new NativeArray<AnimationClipDataBaked>(Clips.Length, Allocator.Persistent);
			for (int i = 0; i < baked.Animations.Count; i++)
			{
				AnimationClipDataBaked data = new AnimationClipDataBaked();
				data.AnimationLength = baked.Animations[i].Clip.length;
				GetTextureRangeAndOffset(baked, baked.Animations[i], out data.TextureRange, out data.TextureOffset, out data.OnePixelOffset, out data.TextureWidth);
				data.Looping = baked.Animations[i].Clip.wrapMode == WrapMode.Loop;

				ClipDataBaked[i] = data;
			}
			
			drawer = new InstancedSkinningDrawer(Material, baked.NewMesh, baked);
		}

		private void OnDisable()
		{
			ClipDataBaked.Dispose();
			drawer.Dispose();
		}

		void LateUpdate()
		{
			drawer.TextureCoordinates.Clear();
			drawer.ObjectToWorld.Clear();
			
			var clipData = ClipDataBaked[animationIndex];
			float normalizedTimeClip = Mathf.Repeat(Time.time, clipData.AnimationLength) / clipData.AnimationLength;
			
			drawer.TextureCoordinates.Add(clipData.ComputeCoordinate(normalizedTimeClip));
			drawer.ObjectToWorld.Add(transform.localToWorldMatrix);

			drawer.Draw();
		}
	}
}