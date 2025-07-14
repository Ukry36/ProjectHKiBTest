using System;

public interface IPoolable
{
    public Action<int, int> OnGameObjectDisabled { get; set; }
    public int PoolSize { get; set; }
    public void OnDisable();
}
