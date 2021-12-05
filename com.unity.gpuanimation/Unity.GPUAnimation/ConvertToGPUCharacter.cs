using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
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

        public static void AddCharacterComponents(IBaker baker, Entity entity, GameObject characterRig, AnimationClip[] clips, float framerate, DynamicBuffer<LinkedEntityGroup> hack)
        {
            //@TODO: Missing Baker.GetComponentInChildren (This is an incorrect dependency setup)
            var lodGroup = baker.Reference(characterRig.GetComponentInChildren<LODGroup>());

            var skinnedMeshRenderers = new List<SkinnedMeshRenderer>();
            if (lodGroup != null)
            {
                foreach (var lod in lodGroup.GetLODs())
                {
                    foreach (var r in lod.renderers)
                    {
                        var skin = baker.Reference(r) as SkinnedMeshRenderer;
                        if (Validate(skin, characterRig))
                            skinnedMeshRenderers.Add(skin);
                    }
                }
            }
            else
            {
                //@TODO: Missing Baker.GetComponentInChildren (This is an incorrect dependency setup)
                var components = characterRig.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var skin in components)
                {
                    baker.Reference(skin);
                    
                    if (Validate(skin, characterRig))
                        skinnedMeshRenderers.Add(skin);
                }
            }

            foreach (var clip in clips)
                baker.Reference(clip);
            foreach (var skin in skinnedMeshRenderers)
                baker.Reference(skin.sharedMesh);
            
            if (skinnedMeshRenderers.Count == 0)
            {
                Debug.LogWarning("No SkinnedMeshRenderers were available to be baked", characterRig);
                return;
            }

            //@TODO: Perform validation that the shader supports GPU Skinning mode
            var bakedData = KeyframeTextureBaker.BakeClips(characterRig, skinnedMeshRenderers.ToArray(), clips, framerate);

            var animState = default(GPUAnimationState);
            animState.AnimationClipSet = CreateClipSet(bakedData);
            baker.AddComponent(entity, animState);
            baker.AddComponent(entity, default(AnimationTextureCoordinate));
            
            baker.AddComponentObject(entity, new ForceIncludeAnimationTextures
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
                    var srcMaterial = baker.Reference(srcMaterials[m]);
                    
                    if (!materials.TryGetValue(srcMaterial, out var material))
                    {
                        material = Object.Instantiate(srcMaterial);
                        material.SetTexture("_AnimationTexture0", bakedData.AnimationTextures.Animation0);
                        material.SetTexture("_AnimationTexture1", bakedData.AnimationTextures.Animation1);
                        material.SetTexture("_AnimationTexture2", bakedData.AnimationTextures.Animation2);
                        materials.Add(srcMaterial, material);
                    }
                    
                    var skinEntity = baker.CreateAdditionalEntity(skinRenderer, TransformUsageFlags.ManualOverride);
                    
                    //@TODO: LinkedEntityGroup is not constructed automatically during baking yet thus we build one manually.
                    hack.Add(skinEntity);
                    
                    baker.AddComponent(skinEntity, new CopyAnimationTextureCoordinate { SourceEntity = entity });
                    baker.AddComponent(skinEntity, default(AnimationTextureCoordinate));
                    
                    // The Skin mesh renderer got baked into a space relative to rig root game object.
                    
                    //@TODO: Would really like to use a higher level TransformAspect utility method to add the right transform components
                    //TransformAspect.AddComponents(manager, skinEntity, entity);
                    baker.AddComponent(skinEntity, new LocalToWorld { Value = characterRig.transform.localToWorldMatrix });
                    baker.AddComponent(skinEntity, new Translation { Value = float3.zero });
                    baker.AddComponent(skinEntity, new Rotation() { Value = quaternion.identity });
                    baker.AddComponent(skinEntity, new Parent() { Value = entity });
                    baker.AddComponent(skinEntity, new LocalToParent() { Value = float4x4.identity });

                    var desc = new RenderMeshDescription(bakedData.BakedMeshes[i], material, skinRenderer.shadowCastingMode, skinRenderer.receiveShadows, skinRenderer.motionVectorGenerationMode, skinRenderer.gameObject.layer, m, skinRenderer.renderingLayerMask);
                    MeshRendererBaker.AddRendererComponents(baker, skinEntity, desc);
                    
                    baker.ConfigureEditorRenderData(skinEntity, skinRenderer.gameObject, true);
                
                    // Our GPU renderer is relative to root transform now.
                    // So we need to transform the bounding volume into root transform space.
                    var transform = characterRig.transform.worldToLocalMatrix * skinRenderer.rootBone.transform.localToWorldMatrix;
                    var aabb = AABB.Transform(transform, skinRenderer.localBounds.ToAABB());
                    baker.SetComponent(skinEntity, new RenderBounds() { Value = aabb });
                }
            }
        }
    }
    public class ConvertToGPUCharacter : MonoBehaviour
    {
        public AnimationClip[] Clips;
        public float Framerate = 60.0F;
    }

    //@TODO: If unity.animation is present, Unity.Animation will want to bake its own SkinnedMeshRenderer entities.
    //       Thats not what we want here. The intention with the ConvertToGPUCharacter is to override all default baking behaviour and let me do my custom stuff.
    //       For example, I don't want there to be a SkinnedMeshRenderer or general purpose animation components to be baked out.
    //       So we need some way to declare that other bakers should be disabled when this one runs.
    class BakeGPUCharacter : Baker<ConvertToGPUCharacter>
    {
        public override void Bake(ConvertToGPUCharacter character)
        {
            var entity = GetEntity(character, TransformUsageFlags.ReadLocalToWorld);
            
            //@TODO: LinkedEntityGroup is not constructed automatically during baking yet thus we build one manually.
            var buf = AddBuffer<LinkedEntityGroup>(entity);
            buf.Add(entity);
            CharacterUtility.AddCharacterComponents(this, entity, character.gameObject, character.Clips, character.Framerate, buf);
        }
    }
}