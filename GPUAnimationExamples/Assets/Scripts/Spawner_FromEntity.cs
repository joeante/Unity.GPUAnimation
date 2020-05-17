using Unity.Entities;

// ReSharper disable once InconsistentNaming
[GenerateAuthoringComponent]
public struct Spawner_FromEntity : IComponentData
{
    public int CountX;
    public int CountY;
    public float Height;
    public Entity Prefab;
}
