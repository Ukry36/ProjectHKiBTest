using UnityEngine;
using UnityEngine.Events;

public class Friendly : Entity, IPoolable
{
    public int ID { get; set; }

    public int PoolSize { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; }

    public FriendlyDataSO BaseData;
    [SerializeField] private DatabaseManagerSO databaseManager;
    public override void Initialize()
    {
        base.Initialize();
        databaseManager.SetIPhysics(this, BaseData);
        databaseManager.SetIAttackable(this, BaseData);
        databaseManager.SetIDamagable(this, BaseData);
        databaseManager.SetIFootstep(this, BaseData);
        databaseManager.SetIPathFindable(this, BaseData);
        databaseManager.SetIAnimatable(this, BaseData);
        InitializeStateMachine(BaseData.StateMachine);
        InitializeModules();
    }

    public void InitializeFromPool(FriendlyDataSO friendlyData)
    {
        BaseData = friendlyData;
    }

    /// <summary>
    /// StateController의 예약 작업을 먼저 정리하고 풀 관리자에 비활성화를 알린다.
    /// 재사용될 때 이전 State의 시간 작업이 남지 않도록 한다.
    /// </summary>
    public override void OnDisable()
    {
        base.OnDisable();
        OnGameObjectDisabled?.Invoke(BaseData.GetInstanceID(), this.gameObject.GetHashCode());
    }
}
