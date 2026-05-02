using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LPBOX : MonoBehaviour
{
    public event Action<int> OnLPClicked;
    private int myId;
    private LP lpScript;
    private Button button;
    public void SetUp(int id)
    {
        myId=id;
        lpScript=GetComponentInChildren<LP>();
        Debug.Log("½ÇÇà2");
        button = GetComponent<Button>();

        button.onClick.AddListener(() => OnLPClicked?.Invoke(id));        
    }
    public void Select() =>lpScript.Active();
    public void Stop() => lpScript.Stop();
}
