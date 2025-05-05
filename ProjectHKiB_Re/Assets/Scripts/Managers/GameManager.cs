using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public AudioManager audioManager;
    public ParticleManager particleManager;
    public DamageParticleManager damageParticleManager;
    public AttackAreaIndicatorManager attackAreaIndicatorManager;
    public InputManager inputManager;
    public PathFindingManager pathFindingManager;

    public Transform player;

    private void Awake()
    {
        instance = this;
    }


}
