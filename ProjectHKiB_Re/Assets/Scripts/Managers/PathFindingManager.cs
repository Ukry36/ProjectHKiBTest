using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{
    private class Node
    {
        public Node(bool isWall, Vector3 pos, int i, int j)
        {
            this.isWall = isWall;
            this.pos = pos;
            this.i = i;
            this.j = j;
            inOpen = false;
        }
        public bool isWall;
        public bool inOpen;
        public int ParentNodeIndex;

        // G : 시작으로부터 이동했던 거리, H : |가로|+|세로| 장애물 무시하여 목표까지의 거리, F : G + H
        public Vector3 pos;
        public int G, H, i, j;
        public int F { get => G + H; }
    }

    private readonly Queue<IEnumerator> PathFindingQueue = new();

    private void Update()
    {
        if (PathFindingQueue.Count > 0)
        {
            StartCoroutine(PathFindingQueue.Dequeue());
        }
    }

    public void PathFindingFull(Action<List<Vector3>> getNodes, Vector3 bottomLeft, Vector3 topRight, Vector3 startPos, Vector3 targetPos, LayerMask wallLayer, bool allowDiagonal = true, bool dontCrossCorner = false)
    {
        PathFindingQueue.Enqueue(PathFindingFullCoroutine(getNodes, bottomLeft, topRight, startPos, targetPos, wallLayer, allowDiagonal, dontCrossCorner));
    }

    public void PathFindingFull(Action<List<Vector3>> getNodes, float radius, Vector3 startPos, Vector3 targetPos, LayerMask wallLayer, bool allowDiagonal = true, bool dontCrossCorner = false)
    => PathFindingFull(getNodes, startPos + Vector3.left * radius + Vector3.down * radius, startPos + Vector3.right * radius + Vector3.up * radius, startPos, targetPos, wallLayer, allowDiagonal, dontCrossCorner);

    private int GetNodeIndex(int x, int y, int width)
    => x + y * width;

    private const int MAX_WIDTH = 59;
    private readonly List<Node> OpenList = new(MAX_WIDTH * MAX_WIDTH);
    private readonly HashSet<Node> ClosedList = new(MAX_WIDTH * MAX_WIDTH);
    private IEnumerator PathFindingFullCoroutine(Action<List<Vector3>> getNodes, Vector3 bottomLeft, Vector3 topRight, Vector3 startPos, Vector3 targetPos, LayerMask wallLayer, bool allowDiagonal = true, bool dontCrossCorner = false)
    {
        if (targetPos.x > topRight.x || targetPos.y > topRight.y || targetPos.x < bottomLeft.x || targetPos.y < bottomLeft.y)
        {
            getNodes.Invoke(null);
            yield break;
        }
        int sizeX, sizeY;
        Node StartNode, TargetNode, CurNode;
        List<Vector3> FinalPathList;
        // NodeArray의 크기 정해주고, isWall, x, y 대입
        sizeX = Mathf.RoundToInt(topRight.x - bottomLeft.x + 1);
        sizeY = Mathf.RoundToInt(topRight.y - bottomLeft.y + 1);
        Node[] NodeArray = new Node[sizeX * sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                Vector3 pos = bottomLeft + Vector3.right * i + Vector3.up * j;
                bool isWall = Physics2D.OverlapPoint(pos, wallLayer);
                NodeArray[GetNodeIndex(i, j, sizeX)] = new Node(isWall, pos, i, j);
            }
        }

        // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
        StartNode = NodeArray[GetNodeIndex(Mathf.RoundToInt(startPos.x - bottomLeft.x), Mathf.RoundToInt(startPos.y - bottomLeft.y), sizeX)];
        TargetNode = NodeArray[GetNodeIndex(Mathf.RoundToInt(targetPos.x - bottomLeft.x), Mathf.RoundToInt(targetPos.y - bottomLeft.y), sizeX)];
        StartNode.isWall = false;
        TargetNode.isWall = false;

        OpenList.Clear();
        OpenList.Add(StartNode);
        ClosedList.Clear();
        FinalPathList = new List<Vector3>(sizeY * sizeX);

        while (OpenList.Count > 0)
        {
            // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H)
                    CurNode = OpenList[i];

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);

            // 마지막
            if (CurNode.pos == TargetNode.pos)
            {
                while (TargetNode.pos != StartNode.pos)
                {
                    FinalPathList.Add(TargetNode.pos);
                    TargetNode = NodeArray[TargetNode.ParentNodeIndex];
                }
                //FinalPathList.Add(StartNode);
                FinalPathList.Reverse();
                //Color color = Color.green * UnityEngine.Random.value + Color.blue * UnityEngine.Random.value + Color.red * UnityEngine.Random.value;
                //for (int i = 0; i < FinalPathList.Count - 1; i++)
                //Debug.DrawLine(FinalPathList[i], FinalPathList[i + 1], color, 0.5f);
                getNodes.Invoke(FinalPathList);
                yield break;
            }

            for (int iShift = -1; iShift <= 1; iShift++)
                for (int jShift = -1; jShift <= 1; jShift++)
                {
                    if (iShift == 0 && jShift == 0) continue;
                    if (iShift == 0 || jShift == 0 || allowDiagonal)
                    {
                        int checkX = iShift + CurNode.i;
                        int checkY = jShift + CurNode.j;
                        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
                        if (checkX >= 0 && checkX < sizeX && checkY >= 0 && checkY < sizeY
                        && !NodeArray[GetNodeIndex(checkX, checkY, sizeX)].isWall
                        && !ClosedList.Contains(NodeArray[GetNodeIndex(checkX, checkY, sizeX)]))
                        {
                            // 대각선 허용시, 벽 사이로 통과 안됨
                            if (allowDiagonal)
                                if (NodeArray[GetNodeIndex(CurNode.i, checkY, sizeX)].isWall
                                && NodeArray[GetNodeIndex(checkX, CurNode.j, sizeX)].isWall)
                                    continue;
                            // 코너를 가로질러 가지 않을시, 이동 중에 수직수평 장애물이 있으면 안됨
                            if (dontCrossCorner)
                                if (NodeArray[GetNodeIndex(CurNode.i, checkY, sizeX)].isWall
                                || NodeArray[GetNodeIndex(checkX, CurNode.j, sizeX)].isWall)
                                    continue;

                            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
                            Node NeighborNode = NodeArray[GetNodeIndex(checkX, checkY, sizeX)];
                            int MoveCost = CurNode.G + 10 + UnityEngine.Random.Range(-5, 6);
                            if (allowDiagonal && checkX != 0 && checkY != 0) MoveCost += 4;

                            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
                            if (MoveCost < NeighborNode.G || !NeighborNode.inOpen)
                            {
                                NeighborNode.G = MoveCost;
                                NeighborNode.H = (Mathf.Abs(NeighborNode.i - TargetNode.i) + Mathf.Abs(NeighborNode.j - TargetNode.j)) * 10;
                                NeighborNode.ParentNodeIndex = GetNodeIndex(CurNode.i, CurNode.j, sizeX);

                                OpenList.Add(NeighborNode);
                                NeighborNode.inOpen = true;
                            }
                        }
                    }
                }
        }
        getNodes.Invoke(null);
    }
}
