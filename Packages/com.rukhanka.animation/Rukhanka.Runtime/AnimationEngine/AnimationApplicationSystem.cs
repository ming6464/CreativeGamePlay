using Unity.Burst;
using Unity.Collections;
using Unity.Deformations;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{

[DisableAutoCreation]
[UpdateAfter(typeof(RukhankaAnimationInjectionSystemGroup))]
partial struct AnimationApplicationSystem: ISystem
{
	private EntityQuery
		boneObjectEntitiesWithParentQuery,
		boneObjectEntitiesNoParentQuery;

	NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>> rigToSkinnedMeshRemapTables;

/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnCreate(ref SystemState ss)
	{
		var eqb0 = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent, Parent>()
			.WithAllRW<LocalTransform>();
		boneObjectEntitiesWithParentQuery = ss.GetEntityQuery(eqb0);

		var eqb1 = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent>()
			.WithNone<Parent>()
			.WithAllRW<LocalTransform>();
		boneObjectEntitiesNoParentQuery = ss.GetEntityQuery(eqb1);
		
		var rq = new EntityQueryBuilder(Allocator.Temp)
			.WithAll<AnimatorEntityRefComponent>()
			.Build(ref ss);
		ss.RequireForUpdate(rq);

		rigToSkinnedMeshRemapTables = new NativeParallelHashMap<Hash128, BlobAssetReference<BoneRemapTableBlob>>(128, Allocator.Persistent);
	}
	
/////////////////////////////////////////////////////////////////////////////////

	[BurstCompile]
	public void OnDestroy(ref SystemState ss)
	{
		rigToSkinnedMeshRemapTables.Dispose();
	}

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
		ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;

		var fillRigToSkinnedMeshRemapTablesJH = FillRigToSkinBonesRemapTableCache(ref ss, ss.Dependency);

		//	Propagate local animated transforms to the entities with and without parents
		var propagateTRSToEntitiesWithParentsJH = PropagateAnimatedBonesToEntitiesTRS(ref ss, runtimeData, boneObjectEntitiesWithParentQuery, true, ss.Dependency);
		var propagateTRSToEntitiesNoParentsJH = PropagateAnimatedBonesToEntitiesTRS(ref ss, runtimeData, boneObjectEntitiesNoParentQuery, false, propagateTRSToEntitiesWithParentsJH);
		
		//	Make corresponding skin matrices for all skinned meshes
		var jh = JobHandle.CombineDependencies(fillRigToSkinnedMeshRemapTablesJH, propagateTRSToEntitiesNoParentsJH);
		var applySkinJobHandle = ApplySkinning(ref ss, runtimeData, jh);

		//	Update render bounds for meshes that request this
		var updateRenderBoundsJH = UpdateRenderBounds(ref ss, runtimeData, ss.Dependency);

		ss.Dependency = JobHandle.CombineDependencies(applySkinJobHandle, updateRenderBoundsJH);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle FillRigToSkinBonesRemapTableCache(ref SystemState ss, JobHandle dependsOn)
	{
		var rigDefinitionComponentLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);

	#if RUKHANKA_DEBUG_INFO
		SystemAPI.TryGetSingleton<DebugConfigurationComponent>(out var dc);
	#endif
		var skinnedMeshWithAnimatorQuery = SystemAPI.QueryBuilder().WithAll<SkinMatrix, AnimatedSkinnedMeshComponent>().Build();
		var skinnedMeshes = skinnedMeshWithAnimatorQuery.ToComponentDataListAsync<AnimatedSkinnedMeshComponent>(ss.WorldUpdateAllocator, dependsOn, out var skinnedMeshFromQueryJH);

		var j = new FillRigToSkinBonesRemapTableCacheJob()
		{
			rigDefinitionArr = rigDefinitionComponentLookup,
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables,
			skinnedMeshes = skinnedMeshes,
		#if RUKHANKA_DEBUG_INFO
			doLogging = dc.logAnimationCalculationProcesses
		#endif
		};

		var rv = j.Schedule(skinnedMeshFromQueryJH);
		return rv;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle UpdateRenderBounds(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var updateSkinnedMeshBoundsJob = new UpdateSkinnedMeshBoundsJob()
		{
			worldBonePoses = runtimeData.worldSpaceBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
		};
		
		var updateSkinnedMeshBoundsJH = updateSkinnedMeshBoundsJob.ScheduleParallel(dependsOn);
		
		var skinnedMeshBoundsComponentLookup = SystemAPI.GetComponentLookup<SkinnedMeshBounds>(true);
		var copyBoundsToChildRenderersJob = new CopySkinnedMeshBoundsToChildRenderers()
		{
			skinnedMeshBoundsLookup = skinnedMeshBoundsComponentLookup
		};
		
		var q = SystemAPI.QueryBuilder()
			.WithAll<RenderBounds, DeformedEntity, ShouldUpdateBoundingBoxTag>()
			.Build();
		
		var copyBoundsToChildRenderersJH = copyBoundsToChildRenderersJob.ScheduleParallel(q, updateSkinnedMeshBoundsJH);
		return copyBoundsToChildRenderersJH;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle PropagateAnimatedBonesToEntitiesTRS(ref SystemState ss, in RuntimeAnimationData runtimeData, EntityQuery eq, bool withParents, JobHandle dependsOn)
	{
		var propagateAnimationJob = new PropagateBoneTransformToEntityTRSJob()
		{
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			boneTransforms = withParents ? runtimeData.animatedBonesBuffer : runtimeData.worldSpaceBonesBuffer,
		};

		var jh = propagateAnimationJob.ScheduleParallel(eq, dependsOn);
		return jh;
	}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	JobHandle ApplySkinning(ref SystemState ss, in RuntimeAnimationData runtimeData, JobHandle dependsOn)
	{
		var rigDefinitionComponentLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
		var cullAnimationsTagComponentLookup = SystemAPI.GetComponentLookup<CullAnimationsTag>(true);

		var animationApplyJob = new ApplyAnimationToSkinnedMeshJob()
		{
			boneTransforms = runtimeData.worldSpaceBonesBuffer,
			entityToDataOffsetMap = runtimeData.entityToDataOffsetMap,
			rigDefinitionLookup = rigDefinitionComponentLookup,
			rigToSkinnedMeshRemapTables = rigToSkinnedMeshRemapTables,
			cullAnimationsTagLookup = cullAnimationsTagComponentLookup
		};
		
		var jh = animationApplyJob.ScheduleParallel(dependsOn);
		return jh;
	}
}
}
