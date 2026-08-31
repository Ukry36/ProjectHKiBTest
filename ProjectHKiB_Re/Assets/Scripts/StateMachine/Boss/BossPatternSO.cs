using System;
using StateMachine;
using UnityEngine;

/// <summary>
/// 보스 패턴 타임라인에서 한 번의 대기와 그 뒤 실행할 Action 묶음을 표현한다.
/// Delay는 이전 항목이 실행된 시점부터 흐르는 스케일 시간이다.
/// </summary>
[Serializable]
public sealed class BossPatternTimelineEntry
{
    [Tooltip("인스펙터에서 이 타임라인 항목의 역할을 알아보기 위한 이름.")]
    [SerializeField]
    private string _label;

    [Tooltip("이전 항목 실행 뒤 이 Action 묶음까지 기다릴 스케일 시간(초).")]
    [SerializeField, Min(0f)]
    private float _delay;

    [Tooltip("Delay가 지난 같은 프레임에 위에서부터 순서대로 실행할 StateAction들.")]
    [SerializeReference, SubclassSelector]
    private StateAction[] _actions = Array.Empty<StateAction>();

    public string Label => _label;
    public float Delay => Mathf.Max(0f, _delay);
    public StateAction[] Actions => _actions;

    /// <summary>
    /// 인스펙터에서 입력된 지연시간과 Action 배열을 안전한 값으로 정리한다.
    /// 런타임에서는 원본 SO를 수정하지 않고 읽기만 한다.
    /// </summary>
    public void Validate()
    {
        _delay = Mathf.Max(0f, _delay);
        _actions ??= Array.Empty<StateAction>();
    }
}

/// <summary>
/// 하나의 보스 패턴을 상대 지연시간 기반 StateAction 타임라인으로 정의한다.
/// 런타임 진행 상태는 보관하지 않으며 BossPatternRunner가 컨트롤러별로 실행한다.
/// </summary>
[CreateAssetMenu(fileName = "BossPattern", menuName = "State Machine/Boss/Boss Pattern")]
public sealed class BossPatternSO : ScriptableObject
{
    [Tooltip("패턴 시작 뒤 순서대로 실행할 상대 지연시간 기반 타임라인.")]
    [SerializeField]
    private BossPatternTimelineEntry[] _timeline = Array.Empty<BossPatternTimelineEntry>();

    [Tooltip("마지막 타임라인 Action 실행 뒤 패턴 완료까지 기다릴 스케일 시간(초).")]
    [SerializeField, Min(0f)]
    private float _recoveryDuration;

    public BossPatternTimelineEntry[] Timeline => _timeline;
    public float RecoveryDuration => Mathf.Max(0f, _recoveryDuration);

    /// <summary>
    /// 모든 항목의 상대 Delay와 후딜레이를 더한 설정상 총 길이를 반환한다.
    /// Action 내부에서 발생하는 별도 대기시간은 포함하지 않는다.
    /// </summary>
    public float TotalDuration
    {
        get
        {
            float totalDuration = RecoveryDuration;
            if (_timeline == null) return totalDuration;

            for (int i = 0; i < _timeline.Length; i++)
            {
                if (_timeline[i] != null)
                    totalDuration += _timeline[i].Delay;
            }

            return totalDuration;
        }
    }

    /// <summary>
    /// 음수 시간과 null 배열을 에디터 저장 전에 안전한 값으로 정리한다.
    /// 각 타임라인 항목의 Action 배열도 함께 초기화한다.
    /// </summary>
    private void OnValidate()
    {
        _recoveryDuration = Mathf.Max(0f, _recoveryDuration);
        _timeline ??= Array.Empty<BossPatternTimelineEntry>();

        for (int i = 0; i < _timeline.Length; i++)
            _timeline[i]?.Validate();
    }
}
