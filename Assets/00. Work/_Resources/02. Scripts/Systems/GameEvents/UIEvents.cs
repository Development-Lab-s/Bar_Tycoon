using System;
using Gamelib.EventSystem;

namespace _00._Work._Resources._02._Scripts.Systems.GameEvents
{
    public static class UIEvents
    {
        /*public static readonly FadeEvent Fade = new FadeEvent();
        public static readonly BlurPanelEvent BlurPanel = new BlurPanelEvent();*/
    }

    /*public class FadeEvent : GameEvent
    {
        public bool IsFadeIn { get; private set; }
        public float Duration { get; private set; }
        public Action OnFadeEnd { get; private set; }
        
        public FadeEvent Init(bool isFadeIn, float duration, Action onFadeEnd = null)
        {
            IsFadeIn = isFadeIn;
            Duration = duration;
            OnFadeEnd = onFadeEnd;
            return this;
        }
    }

    public class BlurPanelEvent : GameEvent
    {
        public bool IsOpen { get; private set; }
        public Action OnBlurEnd { get; private set; }

        public BlurPanelEvent Init(bool isOpen, Action endCallback = null)
        {
            IsOpen = isOpen;
            OnBlurEnd = endCallback;
            return this;
        }
    }*/
}