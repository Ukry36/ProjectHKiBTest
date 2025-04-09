using AYellowpaper.SerializedCollections;
using UnityEngine;
[CreateAssetMenu(fileName = "Damage Data", menuName = "Scriptable Objects/Data/Damage Data", order = 3)]
public class DamageDataSO : ScriptableObject
{
    public float damageCoefficient;
    public DamageTypeSO damageType;
    public float knockBack;
    public LayerMask damageLayer;
    public AudioDataSO initialSound;
    public AudioDataSO hitSound;
    public ParticlePlayer hitParticle;
    public bool camShake;
    public SerializedDictionary<EnumManager.AnimDir, ParticlePlayer> DLRUDamageEffects;
    public BoxCollider2DData downwardDamageArea;
}
