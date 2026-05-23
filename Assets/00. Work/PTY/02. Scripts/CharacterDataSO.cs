using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "SO/CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    public string characterName;
    public Sprite characterImage;  // 왼쪽 큰 이미지
    public Sprite icon;            // 리스트 아이콘
    [TextArea] public string description;
    public string job;
    public string specialty;
    public string hobby;
    public string favoriteCocktail;
}