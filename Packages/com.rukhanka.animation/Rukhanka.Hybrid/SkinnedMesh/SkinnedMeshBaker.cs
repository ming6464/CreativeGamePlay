using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using FixedStringName = Unity.Collections.FixedString512Bytes;
using Hash128 = Unity.Entities.Hash128;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
public struct SkinnedMeshRendererRootBoneEntity: IComponentData
{
	public Entity value;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class SkinnedMeshBaker: Baker<SkinnedMeshRenderer>
{
	public override void Bake(SkinnedMeshRenderer a)
	{
		var smrHash = new Hash128((uint)a.GetInstanceID(), 0, 0, 0);
		var e = GetEntity(a, TransformUsageFlags.None);
		
		var isSMSBlobExists = TryGetBlobAssetReference<SkinnedMeshInfoBlob>(smrHash, out var smrBlobAsset);
		if (!isSMSBlobExists)
		{
			smrBlobAsset = CreateSkinnedMeshBonesBlob(a, smrHash);
			AddBlobAssetWithCustomHash(ref smrBlobAsset, smrHash);
		}
		
		var asmc = new AnimatedSkinnedMeshComponent()
		{
			boneInfos = smrBlobAsset,
			animatedRigEntity = GetEntity(a.gameObject.GetComponentInParent<RigDefinitionAuthoring>(true), TransformUsageFlags.Dynamic),
			rootBoneIndexInRig = -1
		};
		AddComponent(e, asmc);
		
		var rbe = new SkinnedMeshRendererRootBoneEntity()
		{
			value = GetEntity(a.rootBone, TransformUsageFlags.None)
		};
		AddComponent(e, rbe);

	#if RUKHANKA_DEBUG_INFO
		if (a.rootBone == null && a.updateWhenOffscreen)
			Debug.LogError($"Skinned mesh '{a.name}' root bone is null. This will prevent renderer bounding box recalculation! Disable 'Update When Offscreen' or assign valid root bone.");
	#endif
			
		if (a.updateWhenOffscreen && a.rootBone != null)
		{
			var lb = a.localBounds;
			var aabb = new AABB() { Center = lb.center, Extents = lb.extents };
			var smb = new SkinnedMeshBounds() { value = aabb };
			AddComponent(e, smb);
		}
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	BlobAssetReference<SkinnedMeshInfoBlob> CreateSkinnedMeshBonesBlob(SkinnedMeshRenderer r, Hash128 smrHash)
	{ 
		var bb =  new BlobBuilder(Allocator.Temp);
		ref var smrBlob = ref bb.ConstructRoot<SkinnedMeshInfoBlob>();
		smrBlob.hash = smrHash;
	#if RUKHANKA_DEBUG_INFO
		if (r.name.Length > 0)
			bb.AllocateString(ref smrBlob.skeletonName, r.name);
		var startTimeMarker = Time.realtimeSinceStartup;
	#endif
		
		var bonesArr = bb.Allocate(ref smrBlob.bones, r.bones.Length);
		for (int j = 0; j < bonesArr.Length; ++j)
		{
			var b = r.bones[j];
			ref var boneBlob = ref bonesArr[j];
			
			if (b != null)
			{
	#if RUKHANKA_DEBUG_INFO
				bb.AllocateString(ref boneBlob.name, b.name);
	#endif
				var bn = new FixedStringName(b.name);
				boneBlob.hash = bn.CalculateHash128();
				boneBlob.bindPose = r.sharedMesh.bindposes[j];
			}
		}
	#if RUKHANKA_DEBUG_INFO
		var dt = Time.realtimeSinceStartupAsDouble - startTimeMarker;
		smrBlob.bakingTime = (float)dt;
	#endif

		var rv = bb.CreateBlobAssetReference<SkinnedMeshInfoBlob>(Allocator.Persistent);
		return rv;
	}
}
}
