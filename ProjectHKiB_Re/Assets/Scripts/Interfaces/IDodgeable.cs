using UnityEngine;

public interface IDodgeable : IDodgeableBase
{
    public bool CanDodge { get; set; }
    public int DodgeCount { get; set; }
    public bool CanCounterAttack { get; set; }
    public void Initialize();
    public void StartDodge();
    public void StartKeepDodge();
    public bool CheckKeepDodgeEnded();
    public void EndDodge();
}