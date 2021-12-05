using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class SimpleAnimAuthoring :MonoBehaviour
{
    public bool RandomizeStartTime = false;
    public int ClipIndex = 0;
    public float MoveSpeed;
    public float2 RandomzeMinMaxSpeed = new float2(1, 1);
}

class SimpleAnimBaker : Baker<SimpleAnimAuthoring>
{
    public override void Bake(SimpleAnimAuthoring authoring)
    {
        AddComponent( new SimpleAnim
        {
            RandomizeStartTime = authoring.RandomizeStartTime, 
            ClipIndex = authoring.ClipIndex, 
            MovementSpeed = authoring.MoveSpeed,
            Speed = 1.0F, 
            DidInitialize = false,
            RandomizeMinMaxSpeed = authoring.RandomzeMinMaxSpeed
        });
    }
}