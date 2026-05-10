using Assets._00._Work.PCM._02._Scripts._TileChange;
using LitMotion;
using System.Collections;
using UnityEngine;

namespace Assets._00._Work.PCM._02._Scripts.LPPlayer
{
    public class SideLp : LP
    {
        public override void Awake()
        {
            rect = GetComponent<RectTransform>();
            base.Awake();
        }
        public override void ApplyPosition()
        {
            rect.anchoredPosition = Vector3.zero;
        }
        public override void Active()
        {
            moveMotion = LMotion.Create(0f, rect.rect.size.x * 0.5f, 0.5f)
                .WithEase(easeType).WithOnComplete
                (() =>
                {
                })
                .Bind(x =>
                {
                    rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
                });
            
        }

        public override void Stop()
        {
            base.Stop();
        }
    }
}