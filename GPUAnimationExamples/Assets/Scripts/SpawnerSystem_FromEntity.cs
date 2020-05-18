using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
public class SpawnerSystem_FromEntity : SystemBase
{
    //@TODO: This should be builtin functionality
    static void PlaceSingle(EntityManager entityManager, Entity instance, float3 translation, quaternion rotation)
    {
        if (entityManager.HasComponent(instance, ComponentType.ReadWrite<Translation>()))
            entityManager.SetComponentData(instance, new Translation { Value = entityManager.GetComponentData<Translation>(instance).Value + translation });
        if (entityManager.HasComponent(instance, ComponentType.ReadWrite<Rotation>()))
            entityManager.SetComponentData(instance, new Rotation { Value = math.mul(entityManager.GetComponentData<Rotation>(instance).Value, rotation) });
        if (entityManager.HasComponent(instance, ComponentType.ReadWrite<LocalToWorld>()))
        {
            var localToWorld = entityManager.GetComponentData<LocalToWorld>(instance);
            var trs = float4x4.TRS(translation, rotation, new float3(1));
            localToWorld.Value = math.mul(trs, localToWorld.Value);
            entityManager.SetComponentData(instance, localToWorld);
        }
    }

    static void PlacePrefab(EntityManager entityManager, Entity instance, float3 translation, quaternion rotation)
    {
        PlaceSingle(entityManager, instance, translation, rotation);
        
        if (entityManager.HasComponent(instance, ComponentType.ReadWrite<LinkedEntityGroup>()))
        {
            var linked = entityManager.GetBuffer<LinkedEntityGroup>(instance).AsNativeArray();
            for (int i = 1;i < linked.Length;i++)
            {
                var e = linked[i];
                if (!entityManager.HasComponent<Parent>(e.Value))
                    PlaceSingle(entityManager, e.Value, translation, rotation);
            }
        }
    }

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
                float height = spawnerFromEntity.Height;
                LocalToWorld locationCopy = location;

                var entities = EntityManager;
                
                Job.WithCode(() =>
                {
                    var rand = new Random(1);

                    for (var x = 0; x < countX; x++)
                    {
                        for (var y = 0; y < countY; y++)
                        {
                            var rot = quaternion.RotateY(rand.NextFloat(0.0F, math.PI * 2));
                            var position = math.transform(locationCopy.Value,
                                new float3(x * 0.7F, noise.cnoise(new float2(x, y) * 0.21F) * height, y * 0.7F));
                            PlacePrefab(entities, instances[y * countX + x], position, rot);
                        }
                    }
                }).Run();
                instances.Dispose();
            }).Run();
    }
}
