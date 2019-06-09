using Unity.Entities;
using UnityEngine;
class SimpleAnimAuthoring :MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SimpleAnim());
    }
}