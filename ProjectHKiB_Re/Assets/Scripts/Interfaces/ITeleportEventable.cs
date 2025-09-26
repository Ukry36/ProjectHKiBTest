using UnityEngine;

public interface ITeleportEventable : IInitializable
{
    public Transform Destination { get; set; }
    public EnumManager.AnimDir EndDir { get; set; }
}