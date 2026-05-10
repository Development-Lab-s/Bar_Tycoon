using System;
using System.Collections.Generic;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.Codex.Data;
using _00._Work.Goat._02._Scripts.UI.DictonaryUI.CodexDetail;
using UnityEngine;

namespace _00._Work.Goat._02._Scripts.UI.DictonaryUI
{
    public class CodexCanvas : MonoBehaviour
    {
        [Header("codex")]
        [SerializeField] private GameObject codex;
        [SerializeField] private CockTailContent cockTailContent;
        [SerializeField] private CodexManager codexManager;

        [Header("codexDetail")] 
        [SerializeField] private CockTailUI cockTailUI;

        private void Awake()
        {
            cockTailContent.OnClickBtn += HandleOnClickBtn;
            cockTailUI.OnClickExitBtn += HandleOnExitClickBtn;
            codexManager.OnAddCockTail += HandleAddCockTail;
        }

        private void OnDestroy()
        {
            cockTailContent.OnClickBtn -= HandleOnClickBtn;
            cockTailUI.OnClickExitBtn -= HandleOnExitClickBtn;
            codexManager.OnAddCockTail -= HandleAddCockTail;
        }

        private void HandleOnClickBtn(CockTailSlotSo obj)
        {
            codex.SetActive(false);
            cockTailUI.gameObject.SetActive(true);
            cockTailUI.SetView(obj);
        }
        
        private void HandleOnExitClickBtn()
        {
            codex.SetActive(true);
            cockTailUI.gameObject.SetActive(false);
        }
        
        private void HandleAddCockTail(List<CockTailSlotSo> cockTailSOs)
        {
            cockTailContent.SetView(cockTailSOs);
        }
    }
}