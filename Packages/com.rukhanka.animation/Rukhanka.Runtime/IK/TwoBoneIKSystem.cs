using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections.LowLevel.Unsafe;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
    
[UpdateInGroup(typeof(RukhankaAnimationInjectionSystemGroup))]
[UpdateAfter(typeof(AimIKSystem))]
[UpdateAfter(typeof(FABRIKSystem))]
public partial struct TwoBoneIKSystem: ISystem
{
    [BurstCompile]
    partial struct TwoBoneIKJob : IJobEntity
    {
        [ReadOnly]
        public ComponentLookup<RigDefinitionComponent> rigDefLookup;
        [ReadOnly]
        public ComponentLookup<AnimatorEntityRefComponent> animatorEntityRefLookup;
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;
        [ReadOnly]
        public ComponentLookup<Parent> parentLookup;
        
        [NativeDisableContainerSafetyRestriction]
        public RuntimeAnimationData runtimeData;
    
/////////////////////////////////////////////////////////////////////////////////

        void Execute(TwoBoneIKComponent ikc, in AnimatorEntityRefComponent aer)
        {
            if (ikc.weight == 0)
                return;
            
            var rigDef = rigDefLookup[aer.animatorEntity];
            using var animStream = AnimationStream.Create(runtimeData, aer.animatorEntity, rigDef);

            var targetEntityRigRootRelativePose = IKCommon.GetRigRelativeEntityPose(ikc.target, aer.animatorEntity, animStream.GetWorldPose(0), localTransformLookup, parentLookup);
            var hintWorldPos = float4.zero;
            if (ikc.midBentHint != Entity.Null)
            {
                var hintRigRelativePose = IKCommon.GetRigRelativeEntityPose(ikc.midBentHint, aer.animatorEntity, animStream.GetWorldPose(0), localTransformLookup, parentLookup);
                hintWorldPos.xyz = hintRigRelativePose.pos;
                hintWorldPos.w = 1;
            }

            var midEntityRef = animatorEntityRefLookup[ikc.mid];
            var tipEntityRef = animatorEntityRefLookup[ikc.tip];

            var rootWorldPose = animStream.GetWorldPose(aer.boneIndexInAnimationRig);
            var midWorldPose = animStream.GetWorldPose(midEntityRef.boneIndexInAnimationRig);
            var tipWorldPose = animStream.GetWorldPose(tipEntityRef.boneIndexInAnimationRig);
            var targetWorldPos = math.lerp(tipWorldPose.pos, targetEntityRigRootRelativePose.pos, ikc.weight);

            var rootToMidVec = midWorldPose.pos - rootWorldPose.pos;
            var rootToMidVecLen = math.length(rootToMidVec);
            var midToTipVec = tipWorldPose.pos - midWorldPose.pos;
            var midToTipVecLen = math.length(midToTipVec);
            var rootToTipVec = tipWorldPose.pos - rootWorldPose.pos;
            var rootToTipVecLen = math.length(rootToTipVec);
            var rootToTargetVec = targetWorldPos - rootWorldPose.pos;
            var rootToTargetVecLen = math.length(rootToTargetVec);
            
            var curBendAngle = GetAngleFromCosineLaw(rootToMidVecLen, midToTipVecLen, rootToTipVecLen);
            var targetBendAngle = GetAngleFromCosineLaw(rootToMidVecLen, midToTipVecLen, rootToTargetVecLen);

            var bendAxis = math.cross(rootToMidVec, midToTipVec);
            var rootToHintVec = hintWorldPos.xyz - rootWorldPose.pos;
            var bendAxisWithHint = math.cross(rootToHintVec, midToTipVec);
            var useBendAxisWithHint = math.lengthsq(bendAxisWithHint) > math.EPSILON;
            bendAxis = math.select(bendAxis, bendAxisWithHint, useBendAxisWithHint);
            
            var bendAxisLenSqr = math.lengthsq(bendAxis);
            if (bendAxisLenSqr < math.EPSILON)
            {
                bendAxis = math.cross(rootToTargetVec, midToTipVec);
                bendAxisLenSqr = math.lengthsq(bendAxis);
                if (bendAxisLenSqr <= math.EPSILON)
                    bendAxis = math.up();
            }

            bendAxis = math.normalize(bendAxis);
            var deltaAngle = curBendAngle - targetBendAngle;
            var midRotDelta = quaternion.AxisAngle(bendAxis, deltaAngle);
            var midRot = math.mul(midRotDelta, midWorldPose.rot);
            animStream.SetWorldRotation(midEntityRef.boneIndexInAnimationRig, midRot);

            tipWorldPose = animStream.GetWorldPose(tipEntityRef.boneIndexInAnimationRig);
            var updatedRootToTipVec = tipWorldPose.pos - rootWorldPose.pos;
            var rootRotDelta = MathUtils.FromToRotation(updatedRootToTipVec, rootToTargetVec);
            var rootRot = math.mul(rootRotDelta, rootWorldPose.rot);
            animStream.SetWorldRotation(aer.boneIndexInAnimationRig, rootRot);
        }
        
/////////////////////////////////////////////////////////////////////////////////

        float GetAngleFromCosineLaw(float aLen, float bLen, float cLen)
        {
            var cosC = (aLen * aLen + bLen * bLen - cLen * cLen) / (2 * aLen * bLen);
            cosC = math.clamp(cosC, -1.0f, 1.0f);
            var rv = math.acos(cosC);
            return rv;
        }
    }

//==============================================================================//

    [BurstCompile]
    public void OnCreate(ref SystemState ss)
    {
        var q = SystemAPI.QueryBuilder()
            .WithAll<TwoBoneIKComponent, AnimatorEntityRefComponent>()
            .Build();
        
        ss.RequireForUpdate(q);
    }

/////////////////////////////////////////////////////////////////////////////////

    [BurstCompile]
    public void OnUpdate(ref SystemState ss)
    {
        var rigDefLookup = SystemAPI.GetComponentLookup<RigDefinitionComponent>(true);
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);
        var animatorEntityRefLookup = SystemAPI.GetComponentLookup<AnimatorEntityRefComponent>(true);
        ref var runtimeData = ref SystemAPI.GetSingletonRW<RuntimeAnimationData>().ValueRW;

        var ikJob = new TwoBoneIKJob()
        {
            rigDefLookup = rigDefLookup,
            runtimeData = runtimeData,
            localTransformLookup = localTransformLookup,
            animatorEntityRefLookup = animatorEntityRefLookup,
            parentLookup = parentLookup
        };

        ikJob.ScheduleParallel();
    }
}
}
