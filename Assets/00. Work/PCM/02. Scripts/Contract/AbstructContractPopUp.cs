using _00._Work._Resources._02._Scripts.Modules;
using LitMotion;
using LitMotion.Extensions;
using Spine.Unity;
using System.Collections;
using System.Net.NetworkInformation;
using System;
using UnityEngine;
using BBJ.UI;

namespace Assets._00._Work.PCM._02._Scripts.Contract
{
    public enum Uistate
    {
        None,Open, Close 
    }
    public abstract class AbstructContractPopUp : MonoBehaviour, IAbstructContractPopUp
    {
        [SerializeField] protected float animDuration = 0.1f;
        [SerializeField] protected float waitDuration = 2.0f; // 자동 종료 시 대기 시간
        [SerializeField] protected Ease easeType = Ease.OutBack;
        public bool IsAnimating => _motionHandle.IsActive();

        private Vector3 _originScale;
        private Coroutine _timerCoroutine;
        private MotionHandle _motionHandle;

        public bool isOpen { get; set; }

        protected virtual void Awake()
        {
            _originScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }
        public virtual void Start()
        {            
            gameObject.SetActive(false);
        }
        public void Open(bool isAutoClose = false)
        {
            //if (isOpen || IsAnimating) return;

            //isOpen = true;


            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            if (!gameObject.activeSelf)
            {
                OnOpen();
                gameObject.SetActive(true);
            }

            AnimateScale(true, isAutoClose);
        }
        public abstract void OnOpen(); //추상 받고 열때 코드 추가 하고 싶으면 여기다가 추가하셈

        public virtual void OnClose()
        {
            //if (!isOpen || IsAnimating) return;

            //isOpen = false;

            if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            AnimateScale(false);
        }

        private void AnimateScale(bool isAppearing, bool isAutoClose = false)
        {
            if (_motionHandle.IsActive()) _motionHandle.Cancel();

            Vector3 start =isAppearing? Vector3.zero : transform.localScale; 
            Vector3 end = isAppearing ? _originScale : Vector3.zero;

            Ease currentEase = isAppearing ? easeType : Ease.InBack;

            _motionHandle = LMotion.Create(start, end, animDuration)
                .WithEase(currentEase)
                .WithOnComplete(() =>
                {
                    if (isAppearing)
                    {
                        if (isAutoClose)
                        {
                            _timerCoroutine = StartCoroutine(WaitAndClose());
                        }
                    }
                    else
                    {
                        gameObject.SetActive(false);
                        transform.localScale = Vector3.zero;
                    }
                })
                .BindToLocalScale(transform);
        }

        private IEnumerator WaitAndClose()
        {
            yield return new WaitForSeconds(waitDuration);
            OnClose();
        }

        public virtual void OnDisable()
        {
            if (_motionHandle.IsActive())
            {
                _motionHandle.Cancel();
            }
        }


    }
}