using UnityEngine;

public interface ISupply
{
    void Supply(Transform target, int amount);
    public int Amount { get; set; }
    
};

