using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
class SimpleAnimAuthoring :MonoBehaviour, IConvertGameObjectToEntity
{
    public bool RandomizeStartTime = false;
    public int ClipIndex = 0;
    public float2 RandomzeMinMaxSpeed = new float2(1, 1);
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SimpleAnim
        {
            RandomizeStartTime = RandomizeStartTime, 
            ClipIndex = ClipIndex, 
            Speed = 1.0F, 
            IsFirstFrame = true,
            RandomizeMinMaxSpeed = RandomzeMinMaxSpeed
        });
    }
}