using Unity.Collections;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[assembly: RegisterGenericComponentType(typeof(Rukhanka.NewBlobAssetDatabaseRecord<Rukhanka.AnimationClipBlob>))]
[assembly: RegisterGenericComponentType(typeof(Rukhanka.NewBlobAssetDatabaseRecord<Rukhanka.AvatarMaskBlob>))]

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

public struct BlobDatabaseSingleton: IComponentData
{
    public NativeHashMap<Hash128, BlobAssetReference<AnimationClipBlob>> animations;
    public NativeHashMap<Hash128, BlobAssetReference<AvatarMaskBlob>> avatarMasks;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public struct NewBlobAssetDatabaseRecord<T>: IBufferElementData where T: unmanaged
{
    public Hash128 hash;
    public BlobAssetReference<T> value;
}

}

