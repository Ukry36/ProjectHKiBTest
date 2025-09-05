using System.Collections.Generic;
using System.Linq;
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
    public ChunkManager chunkManager;
    public CameraManager cameraManager;
    public CooltimeManager cooltimeManager;
    public EnemyManager enemyManager;
    public ObjectSpawnManager objectSpawnManager;
    public ObjectDeathCountManager objectDeathCountManager;
    public DatabaseManager databaseManager;

    public Player player;

    private void Awake()
    {
        instance = this;
    }

}
