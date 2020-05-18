using Unity.Entities;
using Unity.Transforms;

// ReSharper disable once InconsistentNaming
[GenerateAuthoringComponent]
[TransformUsage(TransformFlags.ReadLocalToWorld)]
public struct Spawner_FromEntity : IComponentData
{
    public int CountX;
    public int CountY;
    public float Height;
    public Entity Prefab;
}
