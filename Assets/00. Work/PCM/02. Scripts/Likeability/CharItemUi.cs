using UnityEngine;
using UnityEngine.Rendering;

public class CharItemUi : MonoBehaviour
{
    [SerializeField] private GameObject DropObject;
    public void DragandDrop()
    {
        GameObject Drag = Instantiate(DropObject);
        Drag.transform.position = transform.position;
    }
}
