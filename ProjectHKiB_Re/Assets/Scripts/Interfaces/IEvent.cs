using System.Collections.Generic;
using UnityEngine;

public interface IEvent : IInitializable
{
    public EventTargets CurrentTargets { get; set; }
}