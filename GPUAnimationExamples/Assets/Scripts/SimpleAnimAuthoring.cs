using Unity.Entities;
using UnityEngine;
class SimpleAnimAuthoring :MonoBehaviour, IConvertGameObjectToEntity
{
    public bool RandomizeStartTime = false;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SimpleAnim { RandomizeStartTime = RandomizeStartTime, IsFirstFrame = true});
    }
}