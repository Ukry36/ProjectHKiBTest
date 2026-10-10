using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

/// <summary>
/// 손전등 1D 그림자 맵(각도 칸별 벽 거리)을 칸마다 병렬로 구한다(Burst).
/// DarknessManager.CastOcclusion / ContinueThroughAdjacentWalls / WallVisualGrid와 같은 계산을 옮긴 것 —
/// 판정을 바꾸면 양쪽을 같이 고칠 것. 벽마다 격자 하나(그림자 판 = 바닥면, 렌더러 = 벽 그림).
/// </summary>
[BurstCompile]
internal struct FlashlightOcclusionJob : IJobParallelFor
{
    public struct Grid
    {
        public float MinX, MinY, Cell, MaxRise;
        public int Width, Height, Offset; // Offset: Below/Clear/Visual 안에서 이 격자가 시작하는 위치
    }

    [ReadOnly] public NativeArray<Grid> Grids;
    [ReadOnly] public NativeArray<ushort> Below;
    [ReadOnly] public NativeArray<byte> Clear;
    [ReadOnly] public NativeArray<byte> Visual;
    public int GridCount;

    public float2 Origin;
    public float Cos, Sin;              // 빛 그림 회전
    public float2 BoundsMin, BoundsMax; // 빛 그림 로컬 범위
    public float Margin, Overshoot;
    public float FootDepth; // 빛이 바닥면(판) 안으로 들어가 밝히는 깊이(앞면)
    public float BottomRadius; // 벽 하단 조명 원 반경
    public float BottomBlend;  // 벽이 광원보다 이만큼 위면 완전히 하단으로 본다
    public int Bins;

    // 칸 i의 결과를 4줄에 쓴다: 그림자 시작 / 벽(판)에 들어간 거리 / 판을 벗어난 거리 / 벽 하단 가중치(0~1).
    [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<float> Distances;

    private const float Nudge = 0.0001f;

    public void Execute(int i)
    {
        float angle = (i + 0.5f) / Bins * math.PI * 2f - math.PI;
        float2 dir = new(math.sin(angle), math.cos(angle));
        float2 local = new(Cos * dir.x + Sin * dir.y, -Sin * dir.x + Cos * dir.y);
        float reach = RayExitAabb(float2.zero, local, BoundsMin, BoundsMax) + Margin;
        float exit = Cast(dir, reach, out float wallEntry, out float footEnd, out float bottom);
        Distances[i] = exit;
        Distances[Bins + i] = wallEntry;
        Distances[2 * Bins + i] = footEnd;
        Distances[3 * Bins + i] = bottom;
    }

    private float Cast(float2 dir, float maxDistance, out float wallEntry, out float footEnd, out float bottom)
    {
        float nearestExit = 1000f;
        wallEntry = footEnd = 1000f;
        bottom = 0f;
        for (int g = 0; g < GridCount; g++)
        {
            Grid grid = Grids[g];
            float hit = FirstFootprintEntry(grid, dir, maxDistance);
            if (hit <= 0.001f) continue;
            // 벽이 광원보다 위일수록 하단(가중치 1). 하단이면 광원 중심 원(BottomRadius)이 더 멀리 닿으면 거기까지 밝힌다.
            float weight = math.smoothstep(0f, 1f, dir.y * hit / BottomBlend);
            float depth = weight > 0f ? math.max(FootDepth, BottomRadius - hit) : FootDepth;
            float exit = math.max(hit, ExitDistance(grid, dir, hit, maxDistance + grid.MaxRise, depth, out float runEnd));
            if (exit >= nearestExit) continue;
            nearestExit = exit;
            wallEntry = hit;
            footEnd = runEnd;
            bottom = weight;
        }
        return ContinueThroughAdjacentWalls(dir, nearestExit, maxDistance);
    }

    private float ContinueThroughAdjacentWalls(float2 dir, float exit, float maxDistance)
    {
        const float probe = 0.02f;
        for (int guard = 0; guard < 8 && exit < maxDistance; guard++)
        {
            float2 p = Origin + dir * (exit + probe);
            // 판 안이면(깊이 제한에 걸려 벽 덩어리 속에서 끊긴 경우) 잇지 않는다.
            bool inFootprint = false;
            for (int g = 0; g < GridCount; g++) inFootprint |= InFootprint(Grids[g], p);
            if (inFootprint) break;
            float next = exit;
            for (int g = 0; g < GridCount; g++)
            {
                Grid grid = Grids[g];
                if (!InVisual(grid, p, out float footY)) continue;
                if (Origin.y >= footY) continue; // 광원이 그 벽 뒤 — 앞면은 빛을 못 받는다
                next = math.max(next, ExitDistance(grid, dir, exit + probe, maxDistance + grid.MaxRise, FootDepth, out _));
            }
            if (next <= exit + probe) break;
            exit = next;
        }
        return exit;
    }

    // ─── 격자 ────────────────────────────────────────────────────

    private struct Walk
    {
        public int X, Y, StepX, StepY;
        public float NextX, NextY, DeltaX, DeltaY;

        public float Step()
        {
            if (NextX < NextY) { float t = NextX; NextX += DeltaX; X += StepX; return t; }
            float ty = NextY; NextY += DeltaY; Y += StepY; return ty;
        }
    }

    // 칸 번호는 시작할 때만 t에서 구하고 그다음은 정수로만 옮긴다(부동소수 오차로 같은 칸에 머무는 무한 루프 방지).
    private Walk BeginWalk(in Grid g, float2 dir, float t)
    {
        float2 p = Origin + dir * (t + Nudge);
        var w = new Walk
        {
            X = (int)math.floor((p.x - g.MinX) / g.Cell),
            Y = (int)math.floor((p.y - g.MinY) / g.Cell),
            StepX = dir.x > 0f ? 1 : -1,
            StepY = dir.y > 0f ? 1 : -1,
        };
        float ax = math.abs(dir.x), ay = math.abs(dir.y);
        w.DeltaX = ax > 1e-6f ? g.Cell / ax : float.PositiveInfinity;
        w.DeltaY = ay > 1e-6f ? g.Cell / ay : float.PositiveInfinity;
        w.NextX = ax > 1e-6f ? (g.MinX + (dir.x > 0f ? w.X + 1 : w.X) * g.Cell - Origin.x) / dir.x : float.PositiveInfinity;
        w.NextY = ay > 1e-6f ? (g.MinY + (dir.y > 0f ? w.Y + 1 : w.Y) * g.Cell - Origin.y) / dir.y : float.PositiveInfinity;
        w.NextX = math.max(w.NextX, t);
        w.NextY = math.max(w.NextY, t);
        return w;
    }

    private static bool InGrid(in Grid g, int x, int y) => x >= 0 && y >= 0 && x < g.Width && y < g.Height;
    private static int MaxSteps(in Grid g) => 4 * (g.Width + g.Height) + 16;

    private float FirstFootprintEntry(in Grid g, float2 dir, float maxDistance)
    {
        float2 gridMin = new(g.MinX, g.MinY), gridMax = new(g.MinX + g.Width * g.Cell, g.MinY + g.Height * g.Cell);
        float tEnter = 0f, tLeave = maxDistance;
        for (int axis = 0; axis < 2; axis++)
        {
            float o = Origin[axis], d = dir[axis];
            if (math.abs(d) < 1e-6f)
            {
                if (o < gridMin[axis] || o > gridMax[axis]) return -1f;
                continue;
            }
            float t1 = (gridMin[axis] - o) / d, t2 = (gridMax[axis] - o) / d;
            tEnter = math.max(tEnter, math.min(t1, t2));
            tLeave = math.min(tLeave, math.max(t1, t2));
        }
        if (tEnter > tLeave) return -1f;

        float t = tEnter;
        Walk w = BeginWalk(g, dir, t);
        // 광원이 이미 바닥면 안이면 그 바닥면을 빠져나간 뒤의 다음 바닥면을 찾는다(맵 벽이 판 하나라 무시하면 전체 그림자가 꺼진다).
        bool leaving = tEnter <= 0f && InGrid(g, w.X, w.Y) && Clear[g.Offset + w.Y * g.Width + w.X] == 0;
        for (int guard = MaxSteps(g); guard > 0 && t <= tLeave; guard--)
        {
            if (InGrid(g, w.X, w.Y))
            {
                int clear = Clear[g.Offset + w.Y * g.Width + w.X];
                if (leaving)
                {
                    if (clear == 0) { t = w.Step(); continue; }
                    leaving = false;
                }
                if (clear == 0) return t;
                if (clear >= 2)
                {
                    t += (clear - 1) * g.Cell;
                    w = BeginWalk(g, dir, t);
                    continue;
                }
            }
            else if (t > tEnter + g.Cell) return -1f;
            t = w.Step();
        }
        return -1f;
    }

    // 바닥면(판) 안을 footDepth만큼 지나면 벽 그림 안이어도 거기서 끊는다(큰 벽 덩어리에 빛이 파고들지 않게).
    // footEnd: start부터 이어진 첫 판 구간이 끝난(또는 깊이 제한에 걸린) 거리.
    private float ExitDistance(in Grid g, float2 dir, float start, float limit, float footDepth, out float footEnd)
    {
        float t = start, inFoot = 0f;
        bool firstRun = true;
        footEnd = start;
        Walk w = BeginWalk(g, dir, t);
        for (int guard = MaxSteps(g); guard > 0 && t < limit; guard--)
        {
            if (!InGrid(g, w.X, w.Y)) return t;
            int i = g.Offset + w.Y * g.Width + w.X;
            if (Visual[i] == 0) return t;
            bool foot = Clear[i] == 0;
            float next = w.Step();
            if (foot)
            {
                if (inFoot + (next - t) >= footDepth)
                {
                    float cut = t + (footDepth - inFoot);
                    if (firstRun) footEnd = cut;
                    return cut;
                }
                inFoot += next - t;
                if (firstRun) footEnd = next;
            }
            else firstRun = false;
            t = next;
        }
        return limit + Overshoot;
    }

    private bool InFootprint(in Grid g, float2 p)
    {
        int x = (int)math.floor((p.x - g.MinX) / g.Cell), y = (int)math.floor((p.y - g.MinY) / g.Cell);
        return InGrid(g, x, y) && Clear[g.Offset + y * g.Width + x] == 0;
    }

    // p가 벽 그림 안인가. footY는 그 아래 가장 가까운 바닥면의 윗변(없으면 -무한).
    private bool InVisual(in Grid g, float2 p, out float footY)
    {
        footY = float.NegativeInfinity;
        int x = (int)math.floor((p.x - g.MinX) / g.Cell), y = (int)math.floor((p.y - g.MinY) / g.Cell);
        if (!InGrid(g, x, y)) return false;
        int i = g.Offset + y * g.Width + x;
        if (Visual[i] == 0) return false;
        int below = Below[i];
        if (below != ushort.MaxValue) footY = below == 0 ? p.y : g.MinY + (y - below + 1) * g.Cell;
        return true;
    }

    // 반직선이 상자를 빠져나가는 거리. 만나지 않으면 0.
    private static float RayExitAabb(float2 origin, float2 dir, float2 min, float2 max)
    {
        float tNear = 0f, tFar = float.MaxValue;
        for (int axis = 0; axis < 2; axis++)
        {
            float o = origin[axis], d = dir[axis];
            if (math.abs(d) < 1e-5f)
            {
                if (o < min[axis] || o > max[axis]) return 0f;
                continue;
            }
            float t1 = (min[axis] - o) / d, t2 = (max[axis] - o) / d;
            tNear = math.max(tNear, math.min(t1, t2));
            tFar = math.min(tFar, math.max(t1, t2));
        }
        return tFar >= tNear ? tFar : 0f;
    }
}
