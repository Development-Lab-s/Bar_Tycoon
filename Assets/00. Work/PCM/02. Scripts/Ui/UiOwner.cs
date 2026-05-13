using _00._Work._Resources._02._Scripts.Modules;
using Assets._00._Work.PCM._02._Scripts;
using Assets._00._Work.PCM._02._Scripts.Contract;
using System;
using System.Collections.Generic;
using Systems;
using UnityEngine;

public class UiOwner : MonoBehaviour , IModule
{
    [SerializeField] private PlayerInputSO _inputSO;
    private Stack<IAbstructContractPopUp> SettingUis = new();
    private Stack<IAbstructContractPopUp> contractUis = new();
    private ModuleOwner _owner;
    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _inputSO.DownPopupClick += HandleUi;
    }
    public void StackAdd(IAbstructContractPopUp contractUi)
    {
        if(contractUi is ISettingUi)
        {
            SettingUis.Push(contractUi);
            return;
        }
        contractUis.Push(contractUi);
    }
    private void HandleUi()
    {
        if(contractUis.Count != 0)
        {
            var settingUi = SettingUis.Peek();
            if(settingUi.IsAnimating)return;

            SettingUis.Pop();
            settingUi.Close();
            return;
        }
        if (contractUis.Count > 0)
        {
            var topUi = contractUis.Peek();
            if (topUi.IsAnimating) return;

            contractUis.Pop();
            topUi.Close();
        }
    }
}
