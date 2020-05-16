using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
public class SpawnerSystem_FromEntity : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithStructuralChanges().ForEach(
            (Entity entity, in Spawner_FromEntity spawnerFromEntity, in LocalToWorld location) =>
            {
                EntityManager.DestroyEntity(entity);

                var instances = EntityManager.Instantiate(spawnerFromEntity.Prefab,
                    spawnerFromEntity.CountX * spawnerFromEntity.CountY, Allocator.TempJob);
                int countX = spawnerFromEntity.CountX;
                int countY = spawnerFromEntity.CountY;
                LocalToWorld locationCopy = location;
                var translations = GetComponentDataFromEntity<Translation>();
                Job.WithCode(() =>
                {
                    for (var x = 0; x < countX; x++)
                    {
                        for (var y = 0; y < countY; y++)
                        {
                            var position = math.transform(locationCopy.Value,
                                new float3(x * 0.7F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 0.7F));
                            translations[instances[y * countX + x]] = new Translation{Value = position};
                        }
                    }
                }).Run();
                instances.Dispose();
            }).Run();
    }
}
