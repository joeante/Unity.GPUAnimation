using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


/////****** TODO: THIS API NEEDS TO BE EXPOSED IN HYBRID RENDERER INSTEAD
namespace Unity.Rendering
{
    internal struct MeshRendererAspect
    {
        public static bool CalculagteNeedMotionVectorPass(MotionVectorGenerationMode mode)
        {
            return mode == MotionVectorGenerationMode.Object || mode == MotionVectorGenerationMode.ForceNoMotion;
        }


        public static void AddComponents(EntityManager dstEntityManager, Entity entity, RenderMesh renderMesh, LightProbeUsage lightProbeUsage, uint renderLayerMask)
        {
            //@TODO:
            var flipWinding = false;

            dstEntityManager.AddComponentData(entity, new PerInstanceCullingTag());
            dstEntityManager.AddComponentData(entity, new RenderBounds { Value = renderMesh.mesh.bounds.ToAABB() });

            if (lightProbeUsage == LightProbeUsage.CustomProvided)
                dstEntityManager.AddComponent<CustomProbeTag>(entity);
            else if (lightProbeUsage == LightProbeUsage.BlendProbes
                     && LightmapSettings.lightProbes != null
                     && LightmapSettings.lightProbes.count > 0)
                dstEntityManager.AddComponent<BlendProbeTag>(entity);
            else
                dstEntityManager.AddComponent<AmbientProbeTag>(entity);

            dstEntityManager.AddSharedComponentData(entity, renderMesh);

            //if (flipWinding)
            //    dstEntityManager.AddComponent(entity, ComponentType.ReadWrite<RenderMeshFlippedWindingTag>());

            //conversionSystem.ConfigureEditorRenderData(entity, meshRenderer.gameObject, true);

            dstEntityManager.AddComponent(entity, ComponentType.ReadOnly<WorldToLocal_Tag>());

            dstEntityManager.AddComponentData(entity, new BuiltinMaterialPropertyUnity_RenderingLayer
            {
                Value = new uint4(renderLayerMask, 0, 0, 0)
            });

            dstEntityManager.AddComponentData(entity, new BuiltinMaterialPropertyUnity_WorldTransformParams
            {
                Value = flipWinding ? new float4(0, 0, 0, -1) : new float4(0, 0, 0, 1)
            });

            // Default initialized light data for URP
            dstEntityManager.AddComponentData(entity, new BuiltinMaterialPropertyUnity_LightData
            {
                Value = new float4(0, 0, 1, 0)
            });
        }
    }
}
