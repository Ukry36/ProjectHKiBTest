using Assets.Scripts.Interfaces.Modules;
using UnityEngine;

[RequireComponent(typeof(GetBuffEventModule))]
public class BuffEventTestModule : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _defaultTarget;
    [SerializeField] private bool _useSelfIfTargetIsNull = true;

    [Header("Test Trigger")]
    [SerializeField] private bool _useKeyInput = true;
    [SerializeField] private KeyCode _testKey = KeyCode.B;
    [SerializeField] private bool _useTriggerEnter = false;

    [Header("Debug")]
    [SerializeField] private bool _showLog = true;

    private GetBuffEventModule _getBuffEventModule;

    private void Awake()
    {
        _getBuffEventModule = GetComponent<GetBuffEventModule>();
    }

    private void Update()
    {
        if (_useKeyInput && Input.GetKeyDown(_testKey))
        {
            ApplyBuffToDefaultTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_useTriggerEnter) return;

        ApplyBuff(other.transform);
    }

    public void ApplyBuffToDefaultTarget()
    {
        Transform target = _defaultTarget;

        if (target == null && _useSelfIfTargetIsNull)
            target = transform;

        ApplyBuff(target);
    }

    public void ApplyBuff(Transform target)
    {
        if (_getBuffEventModule == null)
        {
            Debug.LogError("[BuffEventTestModule] GetBuffEventModule을 찾지 못함.");
            return;
        }

        if (_getBuffEventModule.Buff == null)
        {
            Debug.LogError("[BuffEventTestModule] Buff가 비어 있음.");
            return;
        }

        if (target == null)
        {
            Debug.LogError("[BuffEventTestModule] Target이 null임.");
            return;
        }

        _getBuffEventModule.GetBuff(target, _getBuffEventModule.Buff);

        if (_showLog)
        {
            Debug.Log($"[BuffEventTestModule] {target.name} 에게 {_getBuffEventModule.Buff.name} 버프 적용");
        }
    }
}