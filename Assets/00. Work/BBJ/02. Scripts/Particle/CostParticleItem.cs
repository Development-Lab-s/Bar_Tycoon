using System;
using BBJ.EventSystem;
using BBJ.Scene;
using Gamelib.EventSystem;
using Gamelib.ObjectPool.Runtime;
using Gamelib.SoundSystem;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace BBJ.Particle
{
    public class CostParticleItem : PoolableMono
    {
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private TextMeshPro _text;

        private MotionHandle _moveHandle;
        private MotionHandle _alphaHandle;
        private Action _onComplete;

        private const float MoveDuration = 0.8f;
        private const float MoveDistance = 1f;
        private const float FadeDelay = 0.4f;
        private const float FadeDuration = 0.4f;

        public void Play(int amount, string spriteAssetName, int spriteIndex, Color gainColor, Color spendColor,
            Vector3 worldPos, Action onComplete)
        {
            if (GameSceneManager.Instance?.CurrentScene == SceneType.Main)
                soundChannel.RaiseEvent(new PlaySoundEvent(SfxSounds.CASH_REGISTER_DING, Vector2.zero));
            _onComplete = onComplete;
            transform.position = worldPos;

            Color c = amount >= 0 ? gainColor : spendColor;
            c.a = 1f;
            _text.color = c;
            string sign = amount >= 0 ? "+" : "";
            string spriteTag = string.IsNullOrEmpty(spriteAssetName)
                ? $"<sprite={spriteIndex}>"
                : $"<sprite=\"{spriteAssetName}\" index={spriteIndex}>";
            _text.text = $"{spriteTag}{sign}{amount}";

            if (_moveHandle.IsActive()) _moveHandle.Cancel();
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();

            _moveHandle = LMotion.Create(worldPos, worldPos + Vector3.up * MoveDistance, MoveDuration)
                .WithEase(Ease.OutCubic)
                .BindToPosition(transform);

            _alphaHandle = LMotion.Create(1f, 0f, FadeDuration)
                .WithDelay(FadeDelay)
                .WithOnComplete(() => _onComplete?.Invoke())
                .Bind(a =>
                {
                    Color col = _text.color;
                    col.a = a;
                    _text.color = col;
                });
        }

        public override void ResetItem()
        {
            if (_moveHandle.IsActive()) _moveHandle.Cancel();
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            Color c = _text.color;
            c.a = 1f;
            _text.color = c;
        }
    }
}
