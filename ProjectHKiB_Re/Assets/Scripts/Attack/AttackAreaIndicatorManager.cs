/*
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;

public class AttackAreaIndicatorManager : PoolManager<AttackAreaIndicator>
{
    [SerializeField] private AttackAreaIndicator prefab;
    [SerializeField] private int poolSize;
    private Dictionary<int, Coroutine> coroutines;
    private readonly WaitForSeconds numberDisplayInterval = new(0.05f);
    private readonly Vector3[] damageIndicatorRandomPosLookup = new Vector3[LOOKUPLENGTH];
    private const int LOOKUPLENGTH = 11;

    public void Start()
    {
        Initialize();

        //AttackAreaIndicator p = PlayParticle(allDatas[1].GetInstanceID(), transform, false);
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
        inactiveObjectSet = new(poolSize);
        activeObjectSet = new(poolSize);
        GameObject clone;

        CreatePool(prefab.GetInstanceID(), poolSize);
        for (int j = 0; j < poolSize; j++)
        {
            clone = Instantiate(prefab.gameObject, this.transform);
            if (clone.TryGetComponent(out AttackAreaIndicator indicator))
            {
                AddObjectToPool(prefab.GetInstanceID(), indicator);
                indicator.InitializeFromPool();
            }
            else
            {
                Debug.LogError("ERROR: Failed to create pool(AttackAreaIndicator prefab is invalid)!!!");
            }
        }

        coroutines = new(objects.Count);
    }

    public override void InitObjectOnReuse(AttackAreaIndicator AttackAreaIndicator, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        AttackAreaIndicator.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) AttackAreaIndicator.transform.parent = transform;
    }

    public void StopIndicating(int instanceID)
    {
        StopCoroutine(coroutines[instanceID]);
    }

    public int IndicateAttackArea(AttackAreaIndicatorData indicatorData, Transform transform, Quaternion quaternion)
    {
        AttackAreaIndicator indicator = ReuseObject(prefab.GetInstanceID(), transform, quaternion, false);

        indicator.StartIndicating(indicatorData.downwardIndicatorArea.size, indicatorData.downwardIndicatorArea.offset, indicatorData.downwardIndicatorArea.pivot);
        if (coroutines.ContainsKey(indicator.GetInstanceID()) && coroutines[indicator.GetInstanceID()] != null)
        {
            StopCoroutine(coroutines[indicator.GetInstanceID()]);
        }

        coroutines[indicator.GetInstanceID()] = StartCoroutine(IndicateAttackAreaCoroutine(indicator, indicatorData, transform));
        return indicator.GetInstanceID();
    }

    private IEnumerator IndicateAttackAreaCoroutine(AttackAreaIndicator indicator, AttackAreaIndicatorData indicatorData, Transform transform)
    {
        float startTime = Time.time;
        while (Time.time - startTime < indicatorData.time)
        {
            yield return null;
            indicator.indicatorInner.transform.localPosition = Vector3.Lerp(indicator.indicatorInner.transform.localPosition, indicator.indicatorFrame.transform.localPosition, 0.5f);
            indicator.indicatorInner.size = Vector3.Lerp(indicator.indicatorInner.size, indicator.indicatorFrame.size, 0.5f);
        }

        coroutines[indicator.GetInstanceID()] = null;
        OnObjectUseEnded(prefab.GetInstanceID(), indicator.GetInstanceID());
        indicator.EndIndicating();
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
*/

//* // DOTween ver!
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class AttackAreaIndicatorManager : PoolManager<AttackAreaIndicator>
{
    [SerializeField] private AttackAreaIndicator prefab;
    [SerializeField] private int poolSize;
    private Dictionary<int, Sequence> sequences;

    public void Start()
    {
        Initialize();

        //AttackAreaIndicator p = PlayParticle(allDatas[1].GetInstanceID(), transform, false);
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void InitializePool()
    {
        objects = new();
        inactiveObjectSet = new(poolSize);
        activeObjectSet = new(poolSize);
        GameObject clone;

        CreatePool(prefab.GetInstanceID(), poolSize);
        for (int j = 0; j < poolSize; j++)
        {
            clone = Instantiate(prefab.gameObject, this.transform);
            if (clone.TryGetComponent(out AttackAreaIndicator indicator))
            {
                AddObjectToPool(prefab.GetInstanceID(), indicator);
                indicator.InitializeFromPool();
            }
            else
            {
                Debug.LogError("ERROR: Failed to create pool(AttackAreaIndicator prefab is invalid)!!!");
            }
        }

        sequences = new(objects.Count);
    }

    public override void InitObjectOnReuse(AttackAreaIndicator AttackAreaIndicator, Transform transform, Quaternion rotation, bool attatchToTransform)
    {
        AttackAreaIndicator.transform.SetPositionAndRotation(transform.position, rotation);
        if (attatchToTransform) AttackAreaIndicator.transform.parent = transform;
    }

    public void StopIndicating(int instanceID)
    {
        sequences[instanceID]?.Complete();
    }

    public int IndicateAttackArea(AttackAreaIndicatorData indicatorData, Transform transform, Quaternion quaternion, TweenCallback indicateEndedCallBack = null)
    {
        AttackAreaIndicator indicator = ReuseObject(prefab.GetInstanceID(), transform, quaternion, false);

        indicator.StartIndicating(indicatorData.downwardIndicatorArea.size, indicatorData.downwardIndicatorArea.offset, indicatorData.downwardIndicatorArea.pivot);
        if (sequences.ContainsKey(indicator.GetInstanceID()) && sequences[indicator.GetInstanceID()] != null)
        {
            StopIndicating(indicator.GetInstanceID());
        }

        Sequence sequence = DOTween.Sequence();
        sequence.Join(indicator.indicatorInner.transform.DOLocalMove(indicatorData.downwardIndicatorArea.offset, indicatorData.time));
        sequence.Join(DOTween.To(() => indicator.indicatorInner.size, v => indicator.indicatorInner.size = v, indicatorData.downwardIndicatorArea.size, indicatorData.time));
        sequence.OnComplete(() => { EndIndicatingCallback(indicator); indicateEndedCallBack.Invoke(); });
        DOTween.Play(sequence);
        sequences[indicator.GetInstanceID()] = sequence;
        return indicator.GetInstanceID();
    }

    private void EndIndicatingCallback(AttackAreaIndicator indicator)
    {
        sequences[indicator.GetInstanceID()] = null;
        OnObjectUseEnded(prefab.GetInstanceID(), indicator.GetInstanceID());
        indicator.EndIndicating();
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
//*/