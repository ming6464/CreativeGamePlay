using Unity.Entities;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka
{
public static class ScriptedAnimator
{
    public static void ResetAnimationState(ref DynamicBuffer<AnimationToProcessComponent> atps)
    {
        atps.Clear();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void PlayAnimation
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip,
        float normalizedTime,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip,
            time = normalizedTime,
            weight = weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public static void BlendTwoAnimations
    (
        ref DynamicBuffer<AnimationToProcessComponent> atps,
        BlobAssetReference<AnimationClipBlob> clip0,
        BlobAssetReference<AnimationClipBlob> clip1,
        float normalizedTime,
        float blendFactor,
        float weight = 1,
        BlobAssetReference<AvatarMaskBlob> avatarMask = default
    )
    {
        var atp = new AnimationToProcessComponent()
        {
            animation = clip0,
            time = normalizedTime,
            weight = (1 - blendFactor) * weight,
            avatarMask = avatarMask,
            blendMode = AnimationBlendingMode.Override,
            layerIndex = 0,
            layerWeight = 1,
            motionId = (uint)atps.Length
        };
        atps.Add(atp);
        
        atp.animation = clip1;
        atp.weight = blendFactor * weight;
        atp.motionId = (uint)atps.Length;
        atps.Add(atp);
    }
    
}
}
