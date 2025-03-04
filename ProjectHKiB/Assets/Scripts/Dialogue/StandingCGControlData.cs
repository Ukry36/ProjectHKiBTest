using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

[Serializable]
public class StandingCGControlData
{
    public SpriteLibraryAsset spriteSet;
    public Animation expression;
    public Animation motion;
    public Vector2 targetPos;
    public Vector2 lookDirection;
    public float lookIntensity;
    public bool isTalking;
    public int standingToUse;
}