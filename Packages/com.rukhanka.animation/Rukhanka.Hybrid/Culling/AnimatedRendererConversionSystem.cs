using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Entities.Hybrid.Baking;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[TemporaryBakingType]
internal struct AnimatedRendererBakingComponent: IBufferElementData
{
	public Entity renderEntity;
	public bool needUpdateRenderBounds;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[RequireMatchingQueriesForUpdate]
[UpdateBefore(typeof(SkinnedMeshConversionSystem))]
public partial class AnimatedRendererBakingSystem : SystemBase
{
	[BurstCompile]
	partial struct CreateAnimatedRendererComponentsJob: IJobEntity
	{
		[ReadOnly]
		public BufferLookup<AdditionalEntitiesBakingData> additionalEntitiesBufferLookup;
		[ReadOnly]
		public ComponentLookup<AnimatedSkinnedMeshComponent> animatedSkinnedMeshComponentLookup;
		
		public EntityCommandBuffer ecb;
		
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

		void Execute(Entity e, in DynamicBuffer<AnimatedRendererBakingComponent> buf)
		{
			foreach (var arbc in buf)
			{
				if (arbc.renderEntity == Entity.Null)
					continue;
				
				//	If this is skinned mesh renderer add AnimatedRendererComponent to its additional mesh render entities
				if (animatedSkinnedMeshComponentLookup.HasComponent(arbc.renderEntity))
				{
					if (additionalEntitiesBufferLookup.TryGetBuffer(arbc.renderEntity, out var additionalEntitiesBuf))
					{
						foreach (var ae in additionalEntitiesBuf)
						{
							var arc = new AnimatedRendererComponent() { animatorEntity = e };
							ecb.AddComponent(ae.Value, arc);
							if (arbc.needUpdateRenderBounds)
								ecb.AddComponent<ShouldUpdateBoundingBoxTag>(ae.Value);
						}
					}
				}
				else
				{
					var arc = new AnimatedRendererComponent() { animatorEntity = e };
					ecb.AddComponent(arbc.renderEntity, arc);
				}
			}
		}
	}

//=================================================================================================================//

	protected override void OnUpdate()
	{
		var ecb = new EntityCommandBuffer(CheckedStateRef.WorldUpdateAllocator);
		var q = SystemAPI.QueryBuilder()
			.WithAll<AnimatedRendererBakingComponent>()
			.WithOptions(EntityQueryOptions.IncludePrefab)
			.Build();
		
		var createAnimatedRendererComponentsJob = new CreateAnimatedRendererComponentsJob()
		{
			ecb	= ecb,
			additionalEntitiesBufferLookup = SystemAPI.GetBufferLookup<AdditionalEntitiesBakingData>(true),
			animatedSkinnedMeshComponentLookup = SystemAPI.GetComponentLookup<AnimatedSkinnedMeshComponent>(true)
		};
		
		createAnimatedRendererComponentsJob.Run(q);
		
		ecb.Playback(EntityManager);
	}
} 
}
