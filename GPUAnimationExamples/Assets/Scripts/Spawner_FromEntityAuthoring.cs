using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

// ReSharper disable once InconsistentNaming
//[GenerateAuthoringComponent]
public struct Spawner_FromEntity : IComponentData
{
    public int CountX;
    public int CountY;
    public float Height;
    public Entity Prefab;
}

class Spawner_FromEntityAuthoring : MonoBehaviour
{
    public int CountX;
    public int CountY;
    public float Height;
    public GameObject Prefab;
}

class Spawner_FromEntityAuthoringBaker : Baker<Spawner_FromEntityAuthoring>
{
    public override void Bake(Spawner_FromEntityAuthoring authoring)
    {
        AddComponent(new Spawner_FromEntity
        {
            CountX = authoring.CountX,
            CountY = authoring.CountY,
            Height = authoring.Height,
            Prefab = GetEntity(authoring.Prefab)
        });
    }
}