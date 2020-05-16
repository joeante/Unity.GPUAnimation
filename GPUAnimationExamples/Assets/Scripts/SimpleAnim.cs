using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.GPUAnimation;
using Unity.Mathematics;

struct SimpleAnim : IComponentData
{
    public int  ClipIndex;
    public float Speed;
    public bool IsFirstFrame;
    public bool RandomizeStartTime;
    public float2 RandomizeMinMaxSpeed;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class SimpleAnimSystem : SystemBase
{

    protected override void OnUpdate()
    {
        float DeltaTime = Time.DeltaTime; 
        
        Entities.ForEach((Entity entity, ref GPUAnimationState animstate, ref SimpleAnim simple) =>
        {
            animstate.AnimationClipIndex = simple.ClipIndex;

            ref var clips = ref animstate.AnimationClipSet.Value.Clips;
            if ((uint) animstate.AnimationClipIndex < (uint) clips.Length)
            {
                if (!simple.IsFirstFrame)
                {
                    animstate.Time += DeltaTime * simple.Speed;
                }
                else
                {
                    var length = 10.0F;

                    var random = new Unity.Mathematics.Random((uint)entity.Index + 1);
                    
                    // For more variety randomize state more...
                    random.NextInt();
                    
                    if (simple.RandomizeStartTime)
                        animstate.Time = random.NextFloat(0, length);
                    simple.Speed = random.NextFloat(simple.RandomizeMinMaxSpeed.x, simple.RandomizeMinMaxSpeed.y);
                    
                    simple.IsFirstFrame = false;
                }
            }
            else
            {
                // @TODO: Warnings?
            }
        }).ScheduleParallel();
    }
}