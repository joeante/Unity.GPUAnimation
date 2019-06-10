using Unity.Entities;
using UnityEngine;
class SimpleAnimAuthoring :MonoBehaviour, IConvertGameObjectToEntity
{
    public float Speed = 1.0F;
    public bool RandomizeStartTime = false;
    public int ClipIndex = 0;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SimpleAnim
        {
            RandomizeStartTime = RandomizeStartTime, 
            ClipIndex = ClipIndex, 
            Speed = Speed, 
            IsFirstFrame = true
        });
    }
}