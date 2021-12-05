using Unity.Entities;
using Unity.GPUAnimation;
using Unity.Mathematics;
using Unity.Transforms;

struct SimpleAnim : IComponentData
{
    public int  ClipIndex;
    public float Speed;
    public float MovementSpeed;
    public bool DidInitialize;
    public bool RandomizeStartTime;
    public float2 RandomizeMinMaxSpeed;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SimpleAnimSystem : SystemBase
{

    protected override void OnUpdate()
    {
        float DeltaTime = Time.DeltaTime; 
        
        Entities.ForEach((Entity entity, ref GPUAnimationState animstate, ref SimpleAnim simple, ref Rotation rotation, ref Translation translation) =>
        {
            translation.Value += math.mul(rotation.Value, new float3(0, 0, simple.MovementSpeed) * DeltaTime);
            
            animstate.AnimationClipIndex = simple.ClipIndex;

            ref var clips = ref animstate.AnimationClipSet.Value.Clips;
            if ((uint) animstate.AnimationClipIndex < (uint) clips.Length)
            {
                if (simple.DidInitialize)
                {
                    animstate.Time += DeltaTime * simple.Speed;
                }
                else
                {
                    var length = 10.0F;

                    var random = new Unity.Mathematics.Random((uint)entity.Index + 1);
                    
                    // For more variety randomize state more...
                    random.NextInt();
                    random.NextInt();
                    
                    if (simple.RandomizeStartTime)
                        animstate.Time = random.NextFloat(0, length);
                    simple.Speed = random.NextFloat(simple.RandomizeMinMaxSpeed.x, simple.RandomizeMinMaxSpeed.y);
                    
                    simple.DidInitialize = true;
                }
            }
            else
            {
                // @TODO: Warnings?
            }
        }).ScheduleParallel();
    }
}