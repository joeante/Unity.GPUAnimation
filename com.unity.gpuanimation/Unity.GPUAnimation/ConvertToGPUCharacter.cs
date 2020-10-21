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

		static bool Validate(SkinnedMeshRenderer renderer, GameObject characterRig)
		{
			if (renderer == null)
			{
				Debug.LogWarning("GPU Character rig only supports SkinMeshRenderer as inputs", characterRig);
				return false;
			}
			if (renderer.sharedMesh == null)
			{
				Debug.LogWarning("SkinMeshRenderer.mesh is missing", renderer);
				return false;
			}
			
			var srcMaterials = renderer.sharedMaterials;
			foreach(var material in srcMaterials)
			{
				if (material == null)
				{
					Debug.LogWarning("SkinMeshRenderer material is missing", renderer);
					return false;
				}
			}

			return true;

		}

		static void Depend(GameObjectConversionSystem system, Renderer renderer, GameObject characterRig)
		{
			system.DeclareDependency(characterRig.gameObject, renderer);
			if (renderer != null)
				system.DeclareDependency(renderer.gameObject, characterRig.gameObject);

			var skin = renderer as SkinnedMeshRenderer;
			if (skin)
				system.DeclareAssetDependency(characterRig, skin.sharedMesh);

			var srcMaterials = renderer.sharedMaterials;
			foreach(var m in srcMaterials)
				system.DeclareAssetDependency(characterRig.gameObject, m);
		}

		public static void AddCharacterComponents(GameObjectConversionSystem system, EntityManager manager, Entity entity, GameObject characterRig, AnimationClip[] clips, float framerate)
		{
			var lodGroup = characterRig.GetComponentInChildren<LODGroup>();

			var skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
			if (lodGroup != null)
			{
				foreach (var lod in lodGroup.GetLODs())
				{
					foreach (var r in lod.renderers)
					{
						var skin = r as SkinnedMeshRenderer;
						if (Validate(skin, characterRig))
							skinnedMeshRenderers.Add(skin);
						Depend(system, r, characterRig);
					}
				}
			}
			else
			{
				var components = characterRig.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach (var skin in components)
				{
					if (Validate(skin, characterRig))
						skinnedMeshRenderers.Add(skin);
					
					Depend(system, skin, characterRig);
				}
			}

			foreach(var clip in clips)
				system.DeclareAssetDependency(characterRig, clip);

			if (skinnedMeshRenderers.Count == 0)
			{
				Debug.LogWarning("No SkinnedMeshRenderers were available to be baked", characterRig);
				return;
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

			var materials = new Dictionary<Material, Material>();
			
            for (int i = 0;i != skinnedMeshRenderers.Count;i++)
            {
	            var skinRenderer = skinnedMeshRenderers[i];
				var srcMaterials = skinRenderer.sharedMaterials;
				for (int m = 0;m != srcMaterials.Length;m++)
				{
					var srcMaterial = srcMaterials[m];
					
					if (!materials.TryGetValue(srcMaterial, out var material))
					{
			            material = Object.Instantiate(srcMaterial);
			            material.SetTexture("_AnimationTexture0", bakedData.AnimationTextures.Animation0);
			            material.SetTexture("_AnimationTexture1", bakedData.AnimationTextures.Animation1);
			            material.SetTexture("_AnimationTexture2", bakedData.AnimationTextures.Animation2);
			            materials.Add(srcMaterial, material);
					}
					
					// @TODO: Remove this again once bug in conversion code is fixed. (Sebastian says he'll fix it)
					var skinEntity2 = system.GetPrimaryEntity(skinRenderer.gameObject, TransformUsageFlags.None);
					
					var skinEntity = system.CreateAdditionalEntity(skinRenderer.gameObject, TransformUsageFlags.ManualOverride);
					manager.AddComponentData(skinEntity, new CopyAnimationTextureCoordinate { SourceEntity = entity });
					manager.AddComponentData(skinEntity, default(AnimationTextureCoordinate));
					
					// The Skin mesh renderer got baked into a space relative to rig root game object.
					TransformAspect.AddComponents(manager, skinEntity, entity);
	                manager.AddComponentData(skinEntity, new LocalToWorld { Value = characterRig.transform.localToWorldMatrix });

	                //@TODO: Dependency on the mesh
	                var renderMesh = new RenderMesh
	                {
		                mesh = bakedData.BakedMeshes[i],
		                material = material,
		                layer = skinRenderer.gameObject.layer,
		                subMesh = m,
		                receiveShadows = skinRenderer.receiveShadows,
		                castShadows = skinRenderer.shadowCastingMode,
		                needMotionVectorPass = MeshRendererAspect.CalculagteNeedMotionVectorPass(skinRenderer.motionVectorGenerationMode)
	                };

	                MeshRendererAspect.AddComponents(manager, skinEntity, renderMesh, skinRenderer.lightProbeUsage, skinRenderer.renderingLayerMask);
					system.ConfigureEditorRenderData(skinEntity, skinRenderer.gameObject, true);
	            
					var transform = characterRig.transform.worldToLocalMatrix * skinRenderer.rootBone.transform.localToWorldMatrix;
					var aabb = AABB.Transform(transform, skinRenderer.localBounds.ToAABB());
					manager.SetComponentData(skinEntity, new RenderBounds() { Value = aabb });
				}
            }
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
			// The skin mesh renderer is handled by ConvertToGPUCharacter, don't process any of them.
			Entities.ForEach((Entity entity, SkinnedMeshRenderer renderer) =>
			{
				var ancestor = renderer.GetComponentInParent<ConvertToGPUCharacter>();
				if (ancestor)
				{
					DeclareDependency(ancestor, renderer);
					EntityManager.RemoveComponent<SkinnedMeshRenderer>(entity);
				}
			});

			Entities.ForEach((Entity entity, ConvertToGPUCharacter character) =>
			{
			    CharacterUtility.AddCharacterComponents(this, DstEntityManager, GetPrimaryEntity(character, TransformUsageFlags.ReadLocalToWorld), character.gameObject, character.Clips, character.Framerate);
			});
	    }
    }
}