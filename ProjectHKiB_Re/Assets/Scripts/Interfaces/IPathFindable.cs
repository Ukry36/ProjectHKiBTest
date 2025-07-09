using System.Collections.Generic;
using UnityEngine;

public interface IPathFindable : ITargetable
{
    public Vector3 NextPath { get => IsPathValid ? PathFindController.PathList[0] : default; }
    public bool IsPathValid { get => PathFindController.PathList != null && PathFindController.PathList.Count > 0; }
    public PathFindController PathFindController { get; set; }
}