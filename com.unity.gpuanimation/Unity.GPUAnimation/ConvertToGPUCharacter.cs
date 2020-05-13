using ICSharpCode.NRefactory.Ast;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
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
		

		public static void AddCharacterComponents(EntityManager manager, Entity entity, GameObject characterRig, AnimationClip[] clips, float framerate)
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

			var animState = default(GPUAnimationState);
			animState.AnimationClipSet = CreateClipSet(bakedData);
			manager.AddComponentData(entity, animState);
			manager.AddComponentData(entity, default(AnimationTextureCoordinate));

			var renderCharacter = new RenderCharacter
			{
				Material = renderer.sharedMaterial,
				AnimationTexture = bakedData.AnimationTextures,
				Mesh = bakedData.NewMesh,
				ReceiveShadows = renderer.receiveShadows,
				CastShadows = renderer.shadowCastingMode
				
			};
			manager.AddSharedComponentData(entity, renderCharacter);
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
	    override protected void OnCreate()
	    {
		    base.OnCreate();
		    //Debug.Log("inti");
	    }
	    
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
				CharacterUtility.AddCharacterComponents(DstEntityManager, GetPrimaryEntity(character), character.gameObject, character.Clips, character.Framerate);
		    });
	    }
    }

}