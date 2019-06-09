using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.GPUAnimation;


struct SimpleAnim : IComponentData
{
}

public class SimpleAnimSystem : JobComponentSystem
{
    [BurstCompile]
    [RequireComponentTag(typeof(SimpleAnim))]
    struct SimpleAnimJob : IJobForEach<GPUAnimationState>
    {
        public float DeltaTime;
        public void Execute(ref GPUAnimationState animstate)
        {
            ref var clips = ref animstate.AnimationClipSet.Value.Clips;
            if ((uint) animstate.AnimationClipIndex < (uint) clips.Length)
            {
                if (!animstate.FirstFrame)
                {
                    var length = clips[animstate.AnimationClipIndex].AnimationLength;
                    animstate.Time += DeltaTime;
                }
                else
                {
                    animstate.FirstFrame = false;
                }
            }
            else
            {
                // How to warn???
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new SimpleAnimJob { DeltaTime = Time.deltaTime}.Schedule(this, inputDeps);
    }
}