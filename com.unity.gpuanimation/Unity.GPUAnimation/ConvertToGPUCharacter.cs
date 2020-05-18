using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GPUAnimation
{
	//@TODO: This is a workaround to ensure the generated textures are stored in the subscene
	//       Currently we don't analyze unityengine.objects for their references for inclusion
	class ForceIncludeAnimationTextures : IComponentData, ICloneable
	{
		public AnimationTextures Textures;
		public object Clone()
		{
			return this;
		}
		
	}

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
			var transformConversion = system.World.GetExistingSystem<TransformConversion>();
			transformConversion.DeclareTransformUsage(entity, TransformFlags.ReadLocalToWorld);

			var lodGroup = characterRig.GetComponentInChildren<LODGroup>();

			var skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
			if (lodGroup != null)
			{
				foreach (var lod in lodGroup.GetLODs())
				{
					//@TODO: Validation (More meshes etc)
					skinnedMeshRenderers.Add(lod.renderers[0] as SkinnedMeshRenderer);
				}
			}
			else
			{
				//@TODO: Validate exactly one?
				characterRig.GetComponentsInChildren(skinnedMeshRenderers);
			}

			//@TODO: Perform validation that the shader supports GPU Skinning mode
			var bakedData = KeyframeTextureBaker.BakeClips(characterRig, skinnedMeshRenderers.ToArray(), clips, framerate);

			var animState = default(GPUAnimationState);
			animState.AnimationClipSet = CreateClipSet(bakedData);
			manager.AddComponentData(entity, animState);
			manager.AddComponentData(entity, default(AnimationTextureCoordinate));
			
			manager.AddComponentData(entity, new ForceIncludeAnimationTextures
			{
				Textures = bakedData.AnimationTextures
			});
			
			var materials = new List<Material>();

			bool makeChildEntity = true;
		    var method = typeof(RenderMesh).Assembly.GetType("Unity.Rendering.MeshRendererConversion", true).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            for (int i = 0;i != skinnedMeshRenderers.Count;i++)
            {
	            var skinRenderer = skinnedMeshRenderers[i];
	            
	            //@TODO Share material between lods if same
	            var material = Object.Instantiate(skinRenderer.sharedMaterial);
	            material.SetTexture("_AnimationTexture0", bakedData.AnimationTextures.Animation0);
	            material.SetTexture("_AnimationTexture1", bakedData.AnimationTextures.Animation1);
	            material.SetTexture("_AnimationTexture2", bakedData.AnimationTextures.Animation2);

	            materials.Clear();
				materials.Add(material);

				Entity skinEntity;
				if (makeChildEntity)
				{
					skinEntity = system.CreateAdditionalEntity(skinRenderer);
					manager.AddComponentData(skinEntity, new CopyAnimationTextureCoordinate { SourceEntity = entity });
					manager.AddComponentData(skinEntity, default(AnimationTextureCoordinate));

					system.World.GetExistingSystem<TransformConversion>().DeclareTransformUsage(skinEntity, TransformFlags.ManuallyAddedTransforms);
					
					manager.AddComponentData(skinEntity, new Parent { Value = entity });
	                manager.AddComponentData(skinEntity, new LocalToParent { Value = float4x4.identity });
	                manager.AddComponentData(skinEntity, new LocalToWorld { Value = characterRig.transform.localToWorldMatrix });
				}
				else
				{
					skinEntity = entity;
				}

				
	            method.Invoke(null, new object[]{skinEntity, manager, system, skinRenderer, bakedData.BakedMeshes[i], materials});
	            
	            
	            var transform = characterRig.transform.worldToLocalMatrix * skinRenderer.rootBone.transform.localToWorldMatrix;
	            var aabb = AABB.Transform(transform, skinRenderer.localBounds.ToAABB());
				manager.SetComponentData(skinEntity, new RenderBounds() { Value = aabb });

            }

            //@TODO: Need to expose a public API
            //var method = typeof(RenderMesh).Assembly.GetType("Unity.Rendering.MeshRendererConversion", true).GetMethod("Convert", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            //method.Invoke(null, new object[]{entity, manager, system, renderer, bakedData.NewMesh, materials});
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