using UnityEngine;
[CreateAssetMenu(fileName = "Bullet Data", menuName = "Scriptable Objects/Data/Bullet Data", order = 3)]
public class BulletDataSO : ScriptableObject
{
    public DamageDataSO damageData;
    public float initialSpeed; // 나중에 단위 꼭 붙이기!!
    public float acceleration;
    public float lifeTime;
    public ParticleDataSO trailParticle;
    public bool disappearWhenDamage;
    public bool disappearWhenHit;
    public Sprite sprite;
}
