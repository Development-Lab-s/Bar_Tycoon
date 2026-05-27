using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SkeletonMimic : MonoBehaviour
{
    public MeshFilter skeletonMesh;
    public MeshFilter myFilter { get; private set; }

    void Start()
    {
        myFilter = GetComponent<MeshFilter>();
    }

    void LateUpdate()
    {
        myFilter.mesh = skeletonMesh.sharedMesh;
    }
}
