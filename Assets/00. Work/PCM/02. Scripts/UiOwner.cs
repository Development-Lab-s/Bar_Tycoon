using _00._Work._Resources._02._Scripts.Modules;
using Assets._00._Work.PCM._02._Scripts.Contract;
using System;
using System.Collections.Generic;
using Systems;
using UnityEngine;

public class UiOwner : MonoBehaviour , IModule
{
    [SerializeField] private PlayerInputSO _inputSO;
    private Stack<IAbstructContractPopUp> contractUis;
    private ModuleOwner _owner;
    public void Initialize(ModuleOwner owner)
    {
        _owner = owner;
        _inputSO.DownPopupClick += HandleUi;
    }
    public void StackAdd(IAbstructContractPopUp contractUi,bool OptionUi = false)
    {
        if (!OptionUi)
        {
            contractUis.Push(contractUi);
        }
        else { }
    }
    private void HandleUi()
    {
        var topUi = contractUis.Pop();
        topUi.Close();
    }
}
