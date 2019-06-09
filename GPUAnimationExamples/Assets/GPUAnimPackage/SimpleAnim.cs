using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace GPUAnimPackage
{
    public class SimpleAnim : JobComponentSystem
    {
        //[BurstCompile]
        struct SimpleAnimJob : IJobForEach<AnimationState>
        {
            public float DeltaTime;
            public void Execute(ref AnimationState animstate)
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
}