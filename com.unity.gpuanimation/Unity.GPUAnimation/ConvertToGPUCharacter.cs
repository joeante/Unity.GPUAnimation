using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Unity.GPUAnimation
{
	public static class CharacterUtility
	{
		
		
		public static BlobAssetReference<BakedAnimationClipSet> CreateClipSet(KeyframeTextureBaker.BakedData data)
		{
			using (var builder = new BlobBuilder(Allocator.Temp))
			{
				ref var root = ref builder.ConstructRoot<BakedAnimationClipSet>();
				var clips = builder.Allocate(ref root.Clips, data.Animations.Count);
				for (int i = 0; i != data.Animations.Count; i++)
					clips[i] = new BakedAnimationClip(data.AnimationTextures, data.Animations[i]);

				return builder.CreateBlobAssetReference<BakedAnimationClipSet>(Allocator.Persistent);
			}
		}
		

		public static void AddCharacterComponents(GameObjectConversionSystem system, EntityManager manager, Entity entity, GameObject characterRig, AnimationClip[] clips, float framerate)
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
			var bakedData = KeyframeTextureBaker.BakeClips(characterRig, clips, framerate, lod);

			var materials = new List<Material>();
			materials.Add(renderer.sharedMaterial);

			var animState = default(GPUAnimationState);
			animState.AnimationClipSet = CreateClipSet(bakedData);
			manager.AddComponentData(entity, animState);
			manager.AddComponentData(entity, default(AnimationTextureCoordinate));

			//@TODO: Don't change source material 
			renderer.sharedMaterial.SetTexture("_AnimationTexture0", bakedData.AnimationTextures.Animation0);
            renderer.sharedMaterial.SetTexture("_AnimationTexture1", bakedData.AnimationTextures.Animation1);
            renderer.sharedMaterial.SetTexture("_AnimationTexture2", bakedData.AnimationTextures.Animation2);

            //@TODO: Need to expose a public API
            var method = typeof(RenderMesh).Assembly.GetType("Unity.Rendering.MeshRendererConversion", true).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            method.Invoke(null, new object[]{entity, manager, system, renderer, bakedData.NewMesh, materials});
			//MeshRendererConversion.Convert(entity, manager, system, renderer, bakedData.NewMesh, materials);
		}
	}
    public class ConvertToGPUCharacter : MonoBehaviour
    {
		public AnimationClip[] Clips;
		public float Framerate = 60.0F;
    }

    [UpdateInGroup(typeof(GameObjectBeforeConversionGroup))]
    class ConvertToGPUCharacterSystem : GameObjectConversionSystem
    {
	   override protected void OnUpdate()
	    {
		    //@TODO: need to find a proper solution for this
		    foreach (var system in World.Systems)
		    {
			    if (system.GetType().Name == "SkinnedMeshRendererConversion")
			    {
				    Debug.Log("Did Disable");
				    system.Enabled = false;
			    }
		    }

		    Entities.ForEach((ConvertToGPUCharacter character) =>
		    {
				CharacterUtility.AddCharacterComponents(this, DstEntityManager, GetPrimaryEntity(character), character.gameObject, character.Clips, character.Framerate);
		    });
	    }
    }
}