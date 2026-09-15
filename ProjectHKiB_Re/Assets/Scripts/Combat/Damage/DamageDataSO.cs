using AYellowpaper.SerializedCollections;
using UnityEngine;
[CreateAssetMenu(fileName = "Damage Data", menuName = "Scriptable Objects/Data/Damage Data", order = 3)]
public class DamageDataSO : ScriptableObject
{
    [Tooltip("활성 패링 창에 닿았을 때 피해와 넉백 대신 패링으로 처리할 수 있는 공격인지 설정한다.")]
    [SerializeField]
    private bool _canBeParried;

    public bool CanBeParried => _canBeParried;

    public float damageCoefficient;
    public float knockBack;
    public LayerMask damageLayer;
    public AudioDataSO initialSound;
    public AudioDataSO hitSound;
    public bool camShake;
    public SerializedDictionary<EnumManager.AnimDir, ParticlePlayer> DLRUDamageEffects;
    public string effectAnimationClipName;
    public int animPlayerNumber;
    public bool attatchParticleToBody;
    public BoxData downwardDamageArea;
}
