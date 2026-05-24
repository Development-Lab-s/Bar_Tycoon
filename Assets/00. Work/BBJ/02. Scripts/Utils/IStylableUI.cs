using UnityEngine;

namespace BBJ
{
    public interface IStylableUI
    {
        IStylableUI SetColor(Color color);
        IStylableUI SetCustomShaderFloat(string propertyName, float value);
        IStylableUI SetFlashAmount(float amount);
        IStylableUI SetFlashColor(Color color);
        IStylableUI SetOutlineFade(float fadeValue);
        IStylableUI SetRecolorFade(float fadeValue);
        IStylableUI SetSprite(Sprite sprite);
    }
}