using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGPRecoverBuffable
{
    public CooltimeMultiplierBuffContainer GPRecoverCooltimeBuffer { get; }
    public void RefreshGPRecoverTimer();
}
