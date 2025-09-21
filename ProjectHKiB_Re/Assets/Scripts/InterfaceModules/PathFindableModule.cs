using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class PathFindableModule : TargetableModule, IPathFindable
    {
        public float PathFindCooltime { get; set; }
        public bool IsCooltime { get; set; }

        public Vector3 NextPath { get => IsPathValid ? PathList[0] : default; }
        public bool IsPathValid { get => PathList != null && PathList.Count > 0; }
        private Action<List<Vector3>> getNodesHandler;
        public List<Vector3> PathList { get; set; }
        public MovableModule _movable;
        private Cooltime cooltime = new();
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            base.Register(interfaceRegistable);
            interfaceRegistable.RegisterInterface<IPathFindable>(this);
        }

        protected void Awake()
        {
            getNodesHandler += GetNodes;
        }
        protected void OnDestroy()
        {
            getNodesHandler -= GetNodes;
        }

        private void GetNodes(List<Vector3> pathList)
        {
            PathList = pathList;
        }

        public void Update()
        {
            if (!IsCooltime)
            {
                if (CurrentTarget)
                {
                    GameManager.instance.pathFindingManager.PathFindingFull(getNodesHandler, 12, _movable.MovePoint.transform.position, CurrentTarget.position, _movable.WallLayer);
                }
                cooltime.StartCooltime(PathFindCooltime, () => IsCooltime = false);
                IsCooltime = true;
            }
            if (PathList != null && PathList.Count > 0)
            {
                //for (int i = 0; i < PathList.Count; i++)
                //  Debug.DrawLine(PathList[i] + Vector3.down * 0.25f, PathList[i] + Vector3.up * 0.25f);

                if (PathList[0] == _movable.MovePoint.transform.position)
                {
                    PathList.RemoveAt(0);
                }
            }
        }
    }
}