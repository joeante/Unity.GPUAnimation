using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.GPUAnimation;
using Unity.Mathematics;

struct SimpleAnim : IComponentData
{
    public int  ClipIndex;
    public bool IsFirstFrame;
    public bool RandomizeStartTime;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(CalculateTextureCoordinateSystem))]
public class SimpleAnimSystem : JobComponentSystem
{
    [BurstCompile]
    struct SimpleAnimJob : IJobForEachWithEntity<SimpleAnim, GPUAnimationState>
    {
        public float DeltaTime;
        public void Execute(Entity entity, int index, ref SimpleAnim simple, ref GPUAnimationState animstate)
        {
            animstate.AnimationClipIndex = simple.ClipIndex;

            ref var clips = ref animstate.AnimationClipSet.Value.Clips;
            if ((uint) animstate.AnimationClipIndex < (uint) clips.Length)
            {
                if (!simple.IsFirstFrame)
                {
                    animstate.Time += DeltaTime;
                }
                else
                {
                    var length = 10.0F;

                    var random = new Unity.Mathematics.Random((uint)(index + 1) ^ 323233283 );
                    if (simple.RandomizeStartTime)
                        animstate.Time = random.NextFloat(0, length);
                    
                    simple.IsFirstFrame = false;
                }
            }
            else
            {
                // @TODO: Warnings?
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        return new SimpleAnimJob { DeltaTime = Time.deltaTime}.Schedule(this, inputDeps);
    }
}