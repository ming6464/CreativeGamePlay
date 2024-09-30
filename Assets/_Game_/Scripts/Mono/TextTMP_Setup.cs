using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextTMP_Setup : MonoBehaviour
{
    public Transform textParent;
    public FollowPlayer followPlayer;
    public TextMeshPro textMeshPro;
    public void SetUp(Vector3 offset,bool followPlayer = true)
    {
        if (textParent)
        {
            textParent.localPosition = offset;
        }

        this.followPlayer.enabled = followPlayer;
    }

    public void ChangeText(string text)
    {
        textMeshPro.text = text;
    }

    public void Off()
    {
        gameObject.SetActive(false);
    }
    
}
