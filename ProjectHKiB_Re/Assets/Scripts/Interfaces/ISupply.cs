using UnityEngine;

public interface ISupply : IInitializable
{
    void Supply(Transform target, int amount);
    public int Amount { get; set; }

};

