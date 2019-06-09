using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace GPUAnimPackage
{
	struct AnimationState : IComponentData
	{
		public float NormalizedTime;
		public int   AnimationClipIndex;
		
		public BlobAssetReference<BakedAnimationClipSet> AnimationClipSet;
	}
	
	
	public struct BakedAnimationClipSet
	{
		public BlobArray<BakedAnimationClip> Clips;
	}

	public struct BakedAnimationClip
	{
		public float TextureOffset;
		public float TextureRange;
		public float OnePixelOffset;
		public int   TextureWidth;

		public float AnimationLength;
		public bool  Looping;

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

	struct SharedCharacterState : ISharedComponentData, IEquatable<SharedCharacterState>
	{
		//@TODO: Would be nice if we had BlobAssetReference in shared component data support (Serialize not supported...) 
		public Material                                  Material;
		public AnimationTextures                         AnimationTexture;
		public Mesh                                      Mesh;

		public bool Equals(SharedCharacterState other)
		{
			return Equals(Material, other.Material) && AnimationTexture.Equals(other.AnimationTexture) && Equals(Mesh, other.Mesh);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (ReferenceEquals(Material, null) ? 0 : Material.GetHashCode());
				hashCode = (hashCode * 397) ^ AnimationTexture.GetHashCode();
				hashCode = (hashCode * 397) ^ (ReferenceEquals(Mesh, null) ? 0 : Mesh.GetHashCode());
				return hashCode;
			}
		}
	}

	public static class CharacterUtility
	{
		public static void AddCharacterComponents(EntityManager manager, Entity entity, GameObject characterRig, AnimationClip[] clips)
		{
			var renderer = characterRig.GetComponentInChildren<SkinnedMeshRenderer>();
			
			var lod = new LodData
			{
				Lod1Mesh = renderer.sharedMesh,
				Lod2Mesh = renderer.sharedMesh,
				Lod3Mesh = renderer.sharedMesh,
				Lod1Distance = 0,
				Lod2Distance = 100,
				Lod3Distance = 10000,
			};

			//@TODO: Perform validation that the shader supports GPU Skinning mode
			var bakedData = KeyframeTextureBaker.BakeClips(characterRig, clips, lod);
			
			manager.AddComponentData(entity, default(AnimationState));
			//manager.AddSharedComponentData(entity, new );
			
		}
	}

	public class ConvertCharacter : MonoBehaviour
	{
		public Material Material;
		public GameObject CharacterRig;
		public AnimationClip[] Clips;
		private InstancedSkinningDrawer drawer;
		private KeyframeTextureBaker.BakedData baked;

		NativeArray<BakedAnimationClip> ClipDataBaked;
			
		public int animationIndex;
		
		
		private void GetTextureRangeAndOffset(AnimationTextures animTextures, KeyframeTextureBaker.AnimationClipData clipData, out float range, out float offset, out float onePixelOffset, out int textureWidth)
		{
			float onePixel = 1f / animTextures.Animation0.width;
			float start = (float)clipData.PixelStart / animTextures.Animation0.width + onePixel * 0.5f;
			float end = (float)clipData.PixelEnd / animTextures.Animation0.width + onePixel * 0.5f;
			onePixelOffset = onePixel;
			textureWidth = animTextures.Animation0.width;
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

			ClipDataBaked = new NativeArray<BakedAnimationClip>(Clips.Length, Allocator.Persistent);
			for (int i = 0; i < baked.Animations.Count; i++)
			{
				BakedAnimationClip metaData = new BakedAnimationClip();
				metaData.AnimationLength = baked.Animations[i].Clip.length;
				GetTextureRangeAndOffset(baked.AnimationTextures, baked.Animations[i], out metaData.TextureRange, out metaData.TextureOffset, out metaData.OnePixelOffset, out metaData.TextureWidth);
				metaData.Looping = baked.Animations[i].Clip.wrapMode == WrapMode.Loop;

				ClipDataBaked[i] = metaData;
			}
			
			drawer = new InstancedSkinningDrawer(Material, baked.NewMesh, baked.AnimationTextures);
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