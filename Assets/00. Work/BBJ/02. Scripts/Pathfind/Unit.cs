using System.Collections;
using UnityEngine;
using Pathfind;
using Cysharp.Threading.Tasks;
using System;
public class Unit : MonoBehaviour
{
    public Transform target;
    public PathRequestManager pathRequestManager;
    float speed = 1;
    Vector3[] path;
    int targetIndex;
    private void Start()
    {
        pathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    private void OnPathFound(Vector3[] newPath, bool pathSuccesful)
    {
        if (pathSuccesful)
        {
            path = newPath;
            targetIndex = 0;
            //StopCoroutine("FollowPath");
            FollowPath().Forget();
        }
    }

    private async UniTask FollowPath()
    {
        Vector3 currentWaypoint = path[0];

        while (true)
        {
            if (transform.position == currentWaypoint)
            {
                ++targetIndex;
                if (targetIndex >= path.Length)
                    return;
                currentWaypoint = path[targetIndex];
            }

            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed * Time.deltaTime);
            await UniTask.NextFrame();
        }
    }
    public void OnDrawGizmos()
    {
        if(path != null)
            for(int i = targetIndex; i< path.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(path[i], Vector3.one);

                if (i == targetIndex)
                    Gizmos.DrawLine(transform.position, path[i]);
                else
                    Gizmos.DrawLine( path[i-1], path[i]);
            }    
    }
}
