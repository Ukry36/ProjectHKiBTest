using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class DamageParticleManager : PoolManager<DamageParticlePlayer>
{
    [SerializeField] private DamageParticlePlayer[] allDatas;
    private Dictionary<int, Coroutine> coroutines;
    private readonly WaitForSeconds numberDisplayInterval = new(0.05f);
    private readonly Vector3[] damageIndicatorRandomPosLookup = new Vector3[LOOKUPLENGTH];
    private const int LOOKUPLENGTH = 11;

    public void Start()
    {
        Initialize();

        //DamageParticlePlayer p = PlayParticle(allDatas[1].GetInstanceID(), transform, false);
    }

    public override void Initialize()
    {
        base.Initialize();
        GenerateDamageIndicatorRandomPosLookup();
    }

    public void GenerateDamageIndicatorRandomPosLookup()
    {
        for (int i = 0; i < LOOKUPLENGTH; i++)
        {
            damageIndicatorRandomPosLookup[i] = new(-1 + UnityEngine.Random.value * 2, -1f + (float)i / LOOKUPLENGTH * 2);
        }

    }

    public override void InitializePool()
    {
        objects = new();
        inactiveObjectSet = new(allDatas.Length);
        activeObjectSet = new(allDatas.Length);
        GameObject clone;
        for (int i = 0; i < allDatas.Length; i++)
        {
            CreatePool(allDatas[i].GetInstanceID(), allDatas[i].PoolSize);
            for (int j = 0; j < allDatas[i].PoolSize; j++)
            {
                clone = Instantiate(allDatas[i].gameObject, this.transform);
                if (clone.TryGetComponent(out DamageParticlePlayer particlePlayer))
                {
                    AddObjectToPool(allDatas[i].GetInstanceID(), particlePlayer);
                    particlePlayer.InitializeFromPool();
                }
                else
                {
                    Debug.LogError("ERROR: Failed to create pool(DamageParticlePlayer prefab is invalid)!!!");
                }
            }
        }
        coroutines = new(objects.Count);
    }

    public override void InitObjectOnReuse(DamageParticlePlayer DamageparticlePlayer, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        DamageparticlePlayer.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) DamageparticlePlayer.transform.parent = transform;
    }

    private readonly Vector3 numberShift = Vector3.left * 0.5f;


    public const int MAXDAMAGEDIGITS = 10;

    public void PlayHitParticle(DamageParticleDataSO damageParticleData, int damage, bool isBig, bool isCritical, Transform transform, float damageIndicatorRandomPosInfo)
    {
        Vector3 playerShift = (transform.position - GameManager.instance.player.position).normalized + Vector3.up;

        ParticlePlayer basePlayer;
        if (isCritical)
        {
            if (isBig)
                basePlayer = GameManager.instance.particleManager.PlayParticleOneShot(damageParticleData.BigCriticalHitParticle.GetInstanceID(), transform);
            else
                basePlayer = GameManager.instance.particleManager.PlayParticleOneShot(damageParticleData.SmallCriticalHitParticle.GetInstanceID(), transform);
        }
        else
        {
            if (isBig)
                basePlayer = GameManager.instance.particleManager.PlayParticleOneShot(damageParticleData.BigHitParticle.GetInstanceID(), transform);
            else
                basePlayer = GameManager.instance.particleManager.PlayParticleOneShot(damageParticleData.SmallHitParticle.GetInstanceID(), transform);
        }
        basePlayer.transform.position += playerShift;
        if (damage <= 0)
            return;

        DamageParticlePlayer clone = ReuseObject(damageParticleData.DamageParticlePlayer.GetInstanceID(), transform, Quaternion.identity, false);
        clone.transform.position += playerShift + numberShift
                + damageIndicatorRandomPosLookup[(int)(damageIndicatorRandomPosInfo * LOOKUPLENGTH) % LOOKUPLENGTH];
        if (coroutines.ContainsKey(clone.GetInstanceID()) && coroutines[clone.GetInstanceID()] != null)
        {
            StopCoroutine(coroutines[clone.GetInstanceID()]);
            for (int i = 0; i < MAXDAMAGEDIGITS; i++)
                clone.digits[i].mainParticleSystem.Clear();
        }

        coroutines[clone.GetInstanceID()] = StartCoroutine(PlayNumberParticleCoroutine(clone, damageParticleData, damage, isBig, isCritical, transform, damageIndicatorRandomPosInfo));
    }

    private IEnumerator PlayNumberParticleCoroutine(DamageParticlePlayer particle, DamageParticleDataSO damageParticleData, int damage, bool isBig, bool isCritical, Transform transform, float damageIndicatorRandomPosInfo)
    {
        List<Damageparticle> damageParticleFont = isCritical ?
        damageParticleData.CriticalDamageParticleFont : damageParticleData.DamageParticleFont;

        int[] reversedInts = new int[MAXDAMAGEDIGITS];
        int length = 0;
        while (damage > 0)
        {
            reversedInts[length] = damage % 10;
            damage /= 10;
            length++;
        }
        float fontSize = 0.5f;
        if (isBig) fontSize = 1;

        Vector3 shift = damageParticleFont[0].anim[0].bounds.size.x * fontSize * Vector3.right;
        float startSize = damageParticleFont[0].anim[0].rect.size.x * fontSize * 0.0625f;
        int j = 0;

        for (int i = length - 1; i >= 0; i--)
        {
            ParticleSystem.MainModule main = particle.digits[j].mainParticleSystem.main;
            main.startSize = startSize;
            main.startLifetime = damageParticleFont[0].anim.Length * 0.2f;
            for (int k = 0; k < damageParticleFont[reversedInts[i]].anim.Length; k++)
            {
                particle.digits[j].mainParticleSystem.textureSheetAnimation.SetSprite(k, damageParticleFont[reversedInts[i]].anim[k]);
            }
            particle.digits[j].transform.localPosition = shift * j;
            particle.digits[j].mainParticleSystem.Emit(1);
            j++;
            yield return numberDisplayInterval;
        }
        coroutines[particle.GetInstanceID()] = null;
        OnObjectUseEnded(damageParticleData.DamageParticlePlayer.GetInstanceID(), particle.GetInstanceID());
    }

    public override void ResetPool()
    {
        if (objects != null && objects.Count > 0)
        {
            int[] keys = objects.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
                Destroy(objects[keys[i]].gameObject);
        }
        base.ResetPool();
        //Destroy(_simpleParticleEmitter.gameObject);
        //_simpleParticleEmitter = null;
    }
}