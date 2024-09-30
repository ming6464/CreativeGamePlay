using UnityEditor;

public class CreateScriptTemplate : Editor
{
     [MenuItem("Assets/Create/DOTS Template/ISystem",priority = 0)]
     public static void CreateISystem()
     {
          string templatePath = "Assets/_Game/Scripts/Editor/ISystem.cs.txt";
          ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath,"System.cs");
     }
     
     [MenuItem("Assets/Create/DOTS Template/IComponentData",priority = 0)]
     public static void CreateIComponentData()
     {
          string templatePath = "Assets/_Game/Scripts/Editor/IComponentData.cs.txt";
          ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath,"ComponentData.cs");
     }

     [MenuItem("Assets/Create/DOTS Template/IAspect",priority = 0)]
     public static void CreateIAspect()
     {
          string templatePath = "Assets/_Game/Scripts/Editor/IAspect.cs.txt";
          ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath,"Aspect.cs");
     }
     
     [MenuItem("Assets/Create/DOTS Template/Authoring",priority = 0)]
     public static void CreateAuthoring()
     {
          string templatePath = "Assets/_Game/Scripts/Editor/Authoring.cs.txt";
          ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath,"Authoring.cs");
     }
}