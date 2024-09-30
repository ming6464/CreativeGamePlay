using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class MissingScriptCleaner : MonoBehaviour
{
    [MenuItem("Tools/Clean Missing Scripts")]
    public static void CleanMissingScripts()
    {
        int count = 0;
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t.hideFlags == HideFlags.None && t.gameObject.scene.IsValid())
            {
                count += RemoveMissingScripts(t.gameObject);
            }
        }

        Debug.Log($"Removed {count} missing scripts.");
    }

    private static int RemoveMissingScripts(GameObject go)
    {
        int count = 0;
        Component[] components = go.GetComponents<Component>();

        // Create a list to hold valid components
        List<Component> validComponents = new List<Component>();

        foreach (var component in components)
        {
            // Check if the component is null (missing)
            if (component != null)
            {
                validComponents.Add(component);
            }
            else
            {
                // If the component is null, it means it's a missing script
                count++;
            }
        }

        // If there were missing components, remove them
        if (count > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        return count;
    }
}