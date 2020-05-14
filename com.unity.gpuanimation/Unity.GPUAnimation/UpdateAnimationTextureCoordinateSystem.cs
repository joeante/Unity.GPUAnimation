using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

namespace Unity.GPUAnimation
{
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    public class UpdateAnimationTextureCoordinateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref AnimationTextureCoordinate textureCoordinate, in GPUAnimationState animstate) =>
            {
                ref var clips = ref animstate.AnimationClipSet.Value.Clips;
                if ((uint) animstate.AnimationClipIndex < (uint) clips.Length)
                {
                    var normalizedTime = clips[animstate.AnimationClipIndex].ComputeNormalizedTime(animstate.Time);
                    //@TODO: Hybrid renderer doesn't support float3 instance properties yet
                    textureCoordinate.Coordinate.xyz = clips[animstate.AnimationClipIndex].ComputeCoordinate(normalizedTime);
                }
                else
                {
                    // How to warn???
                }
            }).ScheduleParallel();
        }
    }
}