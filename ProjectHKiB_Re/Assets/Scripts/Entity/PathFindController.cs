using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindController : MonoBehaviour
{
    private Action<List<Vector3>> getNodesHandler;
    public List<Vector3> PathList { get; set; }
    public Transform CurrentTarget { get; set; }
    private IMovable _movable;
    protected void Awake()
    {
        getNodesHandler += GetNodes;
    }
    protected void OnDestroy()
    {
        getNodesHandler -= GetNodes;
    }
    public void Initialize(IMovable movable)
    {
        _movable = movable;
        StartCoroutine(PathfindCoroutine());
    }
    public IEnumerator PathfindCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (CurrentTarget)
            {
                Debug.Log("wtf please fix CurrentTarget issue!!!!!!!!!!!!!!!!!!!");
                GameManager.instance.pathFindingManager.PathFindingFull(getNodesHandler, 12, _movable.MovePoint.transform.position, CurrentTarget.position, _movable.WallLayer);
            }
        }
    }

    private void GetNodes(List<Vector3> pathList)
    {
        PathList = pathList;
    }

    public void Update()
    {
        if (PathList != null && PathList.Count > 0)
        {
            //for (int i = 0; i < PathList.Count; i++)
            //Debug.DrawLine(PathList[i] + Vector3.down * 0.25f, PathList[i] + Vector3.up * 0.25f);

            if (PathList[0].Equals(_movable.MovePoint.transform.position))
            {
                PathList.RemoveAt(0);
            }
        }
    }
}