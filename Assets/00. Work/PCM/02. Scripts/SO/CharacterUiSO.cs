using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterUiSO", menuName = "Scriptable Objects/CharacterUiSO")]
public class CharacterUiSO : ScriptableObject, ISerializationCallbackReceiver
{
    [Serializable]
    public struct ImageParam
    {
        public string imageKey;
        public Sprite sprite;
    }

    [SerializeField]
    private List<ImageParam> imageParams = new();

    public Dictionary<string, Sprite> ImageDict
    { get; private set; } = new();

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        ImageDict.Clear();

        foreach (var param in imageParams)
        {
            if (string.IsNullOrEmpty(param.imageKey))
                continue;

            if (!ImageDict.ContainsKey(param.imageKey))
            {
                ImageDict.Add(param.imageKey, param.sprite);
            }
            else
            {
                Debug.LogWarning(
                    $"중복된 키 발견 : {param.imageKey}");
            }
        }

    }

    public Sprite GetSprite(string key)
    {
        if (ImageDict.TryGetValue(key, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogError(
            $"'{key}' 스프라이트를 찾을 수 없음");

        return null;
    }
}