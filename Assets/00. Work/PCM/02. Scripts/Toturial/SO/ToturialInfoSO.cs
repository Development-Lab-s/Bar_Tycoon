using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ToturialInfoSO", menuName = "Scriptable Objects/ToturialInfoSO")]
public class ToturialInfoSO : ScriptableObject
{
    public string title;
    [TextArea(5,10)]
    public string description;

    public Sprite image;
}
