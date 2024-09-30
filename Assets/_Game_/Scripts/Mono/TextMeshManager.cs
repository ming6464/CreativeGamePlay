using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class TextMeshManager : MonoBehaviour
{
    public TextTMP_Setup textPrefab;
    //
    private List<TextData> _textDataArr;
    private bool           _isAddEvent;

    private void Start()
    {
        _textDataArr = new();
        AddEvent();
    }

    private async void AddEvent()
    {
        while (!_isAddEvent)
        {
            await Task.Yield();
            UpdateHybrid playerSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<UpdateHybrid>();
            if(playerSystem == null) return;
            playerSystem.UpdateText += ChangeText;
            _isAddEvent             =  true;
        }
    }

    private void ChangeText(TextPropertyEvent textProperty)
    {
        bool hasData = false;
        var str = textProperty.text.ToString() + textProperty.number;
        foreach (var textData in _textDataArr)
        {
            if (textData.id == textProperty.id)
            {
                hasData = true;
                if (textProperty.isRemove)
                {
                    textData.textTMP.Off();
                    _textDataArr.Remove(textData);
                }
                else
                {
                    textData.textTMP.ChangeText(str);
                }
                break;
            }
        }

        if (!hasData && !textProperty.isRemove)
        {
            var textNew = Instantiate(textPrefab, textProperty.position, quaternion.identity);
            textNew.SetUp(textProperty.offset,textProperty.textFollowPlayer);
            textNew.ChangeText(str);
            _textDataArr.Add(new TextData()
            {
                id = textProperty.id,
                textTMP = textNew
            });
        }
    }
}

[Serializable]
public struct TextData
{
    public int id;
    public TextTMP_Setup textTMP;
}