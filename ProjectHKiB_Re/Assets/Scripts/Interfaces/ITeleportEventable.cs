using UnityEngine;

public interface ITeleportEventable
{
    public Transform Destination { get; set; }
    public EnumManager.AnimDir EndDir { get; set; }
}