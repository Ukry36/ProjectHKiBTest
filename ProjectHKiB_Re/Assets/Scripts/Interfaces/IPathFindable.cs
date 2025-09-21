using System.Collections.Generic;
using UnityEngine;

public interface IPathFindable : IPathFindableBase
{
    public bool IsCooltime { get; set; }
    public List<Vector3> PathList { get; set; }
    public Vector3 NextPath { get => IsPathValid ? PathList[0] : default; }
    public bool IsPathValid { get => PathList != null && PathList.Count > 0; }
    public void Initialize();
}