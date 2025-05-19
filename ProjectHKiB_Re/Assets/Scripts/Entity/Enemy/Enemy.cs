using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class Enemy : Entity, IAttackAreaIndicatable, IPathFindable, IAttackable, IPoolable, IStateControllable
{
    public AttackDataSO[] AttackDatas { get; set; }
    public StatContainer ATK { get; set; }
    public LayerMask[] TargetLayers { get; set; }
    public Transform CurrentTarget { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; } = 0;

    [field: SerializeField] public AttackController AttackController { get; set; }
    public StatContainer CriticalChanceRate { get; set; }
    public StatContainer CriticalDamageRate { get; set; }
    public int PoolSize { get; set; }

    public delegate void GameObjectDisabled(int ID, int hash);
    public event GameObjectDisabled OnGameObjectDisabled;


    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }
    [field: SerializeField] public AnimationController AnimationController { get; set; }
    [field: SerializeField] public StateController StateController { get; set; }
    public int LastAttackAreaIndicatorID { get; set; }
    public List<Vector3> PathList { get; set; }

    public EnemyDataSO enemyBaseData;
    [SerializeField] private DatabaseManagerSO databaseManager;
    protected override void Awake()
    {
        base.Awake();
        getNodesHandler += GetNodes;
    }
    protected override void OnDestroy()
    {
        base.OnDestroy();
        getNodesHandler -= GetNodes;
    }

    void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        UpdateDatas();
        StartCoroutine(PathfindCoroutine());
        SetAttackController();
    }
    private void SetAttackController()
    => AttackController.SetAttacker(this);

    public void UpdateDatas()
    {
        MovePoint.Initialize();
        databaseManager.SetIMovable(this, enemyBaseData);
        databaseManager.SetIAttackable(this, enemyBaseData);
        databaseManager.SetIDamagable(this, enemyBaseData);
        databaseManager.SetIStateControllable(this, enemyBaseData);
        SetAnimationController();
        SetStateController();
    }
    private void SetAnimationController()
    => AnimationController.animator.runtimeAnimatorController = AnimatorController;
    private void SetStateController()
    {
        StateController.Initialize(StateMachine);
        StateController.RegisterInterface<IMovable>(this);
        StateController.RegisterInterface<IAttackable>(this);
        StateController.RegisterInterface<IAttackAreaIndicatable>(this);
        StateController.RegisterInterface<IPathFindable>(this);
    }

    public void InitializeFromPool(EnemyDataSO enemyData)
    {
        enemyBaseData = enemyData;
    }


    public IEnumerator PathfindCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            if (CurrentTarget)
            {
                GameManager.instance.pathFindingManager.PathFindingFull(getNodesHandler, 12, MovePoint.transform.position, CurrentTarget.position, WallLayer);
            }
        }
    }

    private Action<List<Vector3>> getNodesHandler;
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

            if (PathList[0].Equals(this.MovePoint.transform.position))
            {
                PathList.RemoveAt(0);
            }
        }

    }

    public void OnDisable()
    {
        OnGameObjectDisabled?.Invoke(enemyBaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }

    public override void Die()
    {
        if (LastAttackAreaIndicatorID != 0)
            GameManager.instance.attackAreaIndicatorManager.StopIndicating(LastAttackAreaIndicatorID);
        base.Die();
    }
}