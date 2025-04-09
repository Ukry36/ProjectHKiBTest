using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public AudioManager audioManager;
    public ParticleManager particleManager;
    public InputManager inputManager;

    private void Awake()
    {
        instance = this;
    }


}
