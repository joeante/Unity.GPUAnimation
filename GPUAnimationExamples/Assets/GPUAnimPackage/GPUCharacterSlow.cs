using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GPUAnimPackage
{
	public class GPUCharacterSlow : MonoBehaviour
	{
		public Material Material;
		public GameObject CharacterRig;
		public AnimationClip[] Clips;
		private InstancedSkinningDrawer drawer;
		private KeyframeTextureBaker.BakedData baked;

		public int animationIndex;
		
		NativeArray<BakedAnimationClip> ClipDataBaked;
		
		
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
				ClipDataBaked[i] = new BakedAnimationClip(baked.AnimationTextures, baked.Animations[i]);
			
			drawer = new InstancedSkinningDrawer(Material, baked.NewMesh, baked.AnimationTextures);
		}

		private void OnDisable()
		{
			ClipDataBaked.Dispose();
			drawer.Dispose();
		}
		
		void LateUpdate()
		{
			var clipData = ClipDataBaked[animationIndex];
			float normalizedTimeClip = Mathf.Repeat(Time.time, clipData.AnimationLength) / clipData.AnimationLength;
			
			var localToWorld = new NativeArray<float4x4>(1, Allocator.Temp);
			var texCoords = new NativeArray<float3>(1, Allocator.Temp);

			texCoords[0] = clipData.ComputeCoordinate(normalizedTimeClip);
			localToWorld[0] = transform.localToWorldMatrix;
			
			drawer.Draw(texCoords, localToWorld);
		}
	}
}