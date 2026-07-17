using System;
using UnityEngine.Events;

public interface IPoolable
{
    public int ID { get; set; }
    public UnityEvent<int, int> OnGameObjectDisabled { get; set; }
    public int PoolSize { get; set; }
    public void OnDisable();
}
