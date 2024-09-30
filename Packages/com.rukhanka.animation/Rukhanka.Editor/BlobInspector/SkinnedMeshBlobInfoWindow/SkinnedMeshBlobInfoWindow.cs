using Rukhanka;
using Rukhanka.Editor;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhaka.Editor
{
public class SkinnedMeshBlobInfoWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset = default;
    
    [SerializeField]
    private VisualTreeAsset boneInfoAsset = default;
    
    [SerializeField]
    private VisualTreeAsset entityRefAsset = default;
    
    internal static BlobInspector.BlobAssetInfo skinnedMeshBlob;
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var doc = visualTreeAsset.Instantiate();
        root.Add(doc);
        
        titleContent = new GUIContent("Rukhanka.Animation Skinned Mesh Mask Info");
        FillSkinnedMeshInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void FillSkinnedMeshInfo()
    {
        ref var b = ref skinnedMeshBlob.blobAsset.Reinterpret<SkinnedMeshInfoBlob>().Value;
        var hashLabel = rootVisualElement.Q<Label>("hashLabel");
        hashLabel.text = b.hash.ToString();
        
        var nameLabel = rootVisualElement.Q<Label>("nameLabel");
        var bakingTimeLabel = rootVisualElement.Q<Label>("bakingTimeLabel");
    #if RUKHANKA_DEBUG_INFO
        nameLabel.text = b.skeletonName.ToString();
        bakingTimeLabel.text = $"{b.bakingTime:F3} sec";
    #else
        nameLabel.text = "-";
        bakingTimeLabel.text = "-";
    #endif
        
        var sizeLabel = rootVisualElement.Q<Label>("sizeLabel");
        sizeLabel.text = BlobInspector.FormatMemory(skinnedMeshBlob.blobAsset.m_data.Header->Length);
        
        ref var bones = ref b.bones;
        
        //  Fill bone data
        var bonesFoldout = rootVisualElement.Q<Foldout>("bonesFoldout");
        bonesFoldout.text = $"{bones.Length} Bones";
        
        bonesFoldout.Add(boneInfoAsset.Instantiate());
        for (var i = 0; i < bones.Length; ++i)
        {
            ref var bone = ref bones[i];
            var boneInfoUIEntry = boneInfoAsset.Instantiate();
            if (i % 2 == 0)
                boneInfoUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            var hashLabelB = boneInfoUIEntry.Q<Label>("hashLabel");
            var nameLabelB = boneInfoUIEntry.Q<Label>("nameLabel");
            //var bindPoseLabelB = boneInfoUIEntry.Q<Label>("bindPoseLabel");
            
            hashLabelB.text = bone.hash.ToString();
        #if RUKHANKA_DEBUG_INFO
            nameLabelB.text = bone.name.ToString();
        #else
            nameLabelB.text = "-";
        #endif
            //bindPoseLabelB.text = bone.bindPose.ToString();
            
            bonesFoldout.Add(boneInfoUIEntry);
        }
        
        //  Referenced entities view
        var relatedEntitiesView = rootVisualElement.Q("relatedEntitiesView");
        var relatedEntitieLabel = rootVisualElement.Q<Label>("relatedEntitiesLabel");
        AnimatorClipBlobInfoWindow.PopulateReferencedEntities(relatedEntitiesView, relatedEntitieLabel, skinnedMeshBlob.refEntities, entityRefAsset);
    }
}
}