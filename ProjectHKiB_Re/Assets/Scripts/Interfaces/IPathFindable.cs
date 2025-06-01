using System.Collections.Generic;
using UnityEngine;

public interface IPathFindable
{
    public List<Vector3> PathList { get => PathFindController.PathList; }
    public PathFindController PathFindController { get; set; }
}