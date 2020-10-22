using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Transforms.LowLevel;

public class SpawnerSystem_FromEntity : SystemBase
{
    EntityCommandBufferSystem _CommandsSystem;
    protected override void OnUpdate()
    {
        var buf = _CommandsSystem.CreateCommandBuffer();
        Entities.ForEach(
            (Entity entity, in Spawner_FromEntity spawnerFromEntity, in GlobalTranslation location) =>
            {
                buf.DestroyEntity(entity);

                int countX = spawnerFromEntity.CountX;
                int countY = spawnerFromEntity.CountY;
                float height = spawnerFromEntity.Height;
                var rand = new Random(1);

                for (var x = 0; x < countX; x++)
                {
                    for (var y = 0; y < countY; y++)
                    {
                        var rot = quaternion.RotateY(rand.NextFloat(0.0F, math.PI * 2));
                        var position = location.Value + new float3(x * 0.7F, noise.cnoise(new float2(x, y) * 0.21F) * height, y * 0.7F);

                        buf.Instantiate(spawnerFromEntity.Prefab, position, rot);
                    }
                }
            }).Run();
        
        
    }

    protected override void OnCreate()
    {
        _CommandsSystem = World.GetExistingSystem<BeginInitializationEntityCommandBufferSystem>();
    }
}
