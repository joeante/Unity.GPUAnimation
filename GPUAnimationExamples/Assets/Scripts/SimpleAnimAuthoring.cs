using Unity.Entities;
using UnityEngine;
class SimpleAnimAuthoring :MonoBehaviour, IConvertGameObjectToEntity
{
    public bool RandomizeStartTime = false;
    public int ClipIndex;
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SimpleAnim { RandomizeStartTime = RandomizeStartTime, ClipIndex = ClipIndex, IsFirstFrame = true});
    }
}