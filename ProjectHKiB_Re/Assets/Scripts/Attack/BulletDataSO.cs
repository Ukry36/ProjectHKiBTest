using UnityEngine;
[CreateAssetMenu(fileName = "Bullet Data", menuName = "Scriptable Objects/Data/Bullet Data", order = 3)]
public class BulletDataSO : ScriptableObject
{
    public SimpleAnimationDataSO bulletAnimation;
    public ParticlePlayer trailParticle;
    public DamageDataSO damageData;
    public int pierce;
    public bool stopWhenAttacked;
    public int attackMoveMinRange;
    public int attackMoveMaxRange;
    public bool isAutoTarget;
    public AudioDataSO initialSound;
    public AttackAreaIndicatorData attackAreaIndicatorData;
    public float initialSpeedmps; 
    public float velocitympss;
    public float lifeTimesecond;
}
