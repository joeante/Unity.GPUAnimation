using Unity.Mathematics;
using UnityEngine;

namespace GPUAnimPackage
{
	public class ConvertCharacter : MonoBehaviour
	{
		public Material Material;
		public SkinnedMeshRenderer Renderer;
		public AnimationClip[] Clips;
		private InstancedSkinningDrawer drawer;
		private KeyframeTextureBaker.BakedData baked;
		void OnEnable ()
		{
			var lod = new LodData
			{
				Lod1Mesh = Renderer.sharedMesh,
				Lod2Mesh = Renderer.sharedMesh,
				Lod3Mesh = Renderer.sharedMesh,
				Lod1Distance = 0,
				Lod2Distance = 100,
				Lod3Distance = 10000,
			};

			baked = KeyframeTextureBaker.BakeClips(Renderer, Clips, lod);

			drawer = new InstancedSkinningDrawer(Material, baked.NewMesh, baked);
		}

		private void OnDisable()
		{
			drawer.Dispose();
		}

		void LateUpdate()
		{
			drawer.ObjectPositions.Clear();
			drawer.ObjectRotations.Clear();
			drawer.TextureCoordinates.Clear();

			drawer.ObjectPositions.Add(new float4(transform.position, transform.lossyScale.x));
			drawer.ObjectRotations.Add(transform.rotation);
			float clipLength = baked.Animations[0].Clip.length;
			drawer.TextureCoordinates.Add(new float3(Mathf.Repeat(Time.time, clipLength) / clipLength, 0, 0));

		
			drawer.Draw();
		}
	}
}