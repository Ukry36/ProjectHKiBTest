public interface IPoolable
{
    public delegate void GameObjectDisabled(int ID, int hash);
    public int PoolSize { get; set; }
    public void OnDisable();
}
