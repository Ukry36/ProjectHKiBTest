using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TimerManager에 등록되어 Update 기반으로 진행되는 범용 타이머다.
/// 남은 시간 조회와 재계산, 취소를 한 객체에서 관리한다.
/// </summary>
[Serializable]
public class Timer
{
    public float Time { get; private set; }
    public bool IsCooltimeEnded { get; private set; }

    [Tooltip("타이머가 자연 만료했을 때 실행할 콜백이다.")]
    private Action _timeEndCallback;

    /// <summary>
    /// 현재 타이머가 시작된 뒤 흐른 스케일 시간을 반환한다.
    /// 매니저가 준비되지 않았거나 등록이 끝났다면 0을 반환한다.
    /// </summary>
    public float ElapsedTime
    {
        get
        {
            if (GameManager.instance == null || GameManager.instance.timerManager == null) return 0f;
            return GameManager.instance.timerManager.GetElapsedTime(GetHashCode());
        }
    }

    public float RemainTime => Mathf.Max(0f, Time - ElapsedTime);

    /// <summary>
    /// 지정한 시간으로 타이머를 새로 시작한다.
    /// 이미 진행 중이면 이전 예약을 취소하고 새 콜백으로 교체한다.
    /// </summary>
    public void StartTimer(float cooltime, Action timerEndCallback = null)
    {
        Time = Mathf.Max(0f, cooltime);

        if (!IsCooltimeEnded)
            CancelTimer();

        IsCooltimeEnded = false;
        _timeEndCallback = () =>
        {
            // 종료 상태를 먼저 기록해야 사용자 콜백 안에서 같은 Timer를 다시 시작해도
            // 이전 만료 처리가 새 타이머를 종료 상태로 덮어쓰지 않는다.
            IsCooltimeEnded = true;
            timerEndCallback?.Invoke();
        };

        GameManager.instance.timerManager.StartCooltime(GetHashCode(), this, _timeEndCallback);
    }

    /// <summary>
    /// 현재까지 흐른 시간은 유지하고 총 타이머 시간만 다시 계산한다.
    /// 새 총 시간이 이미 지난 시간 이하라면 즉시 만료 콜백을 실행한다.
    /// </summary>
    public void RecalculateTotalTime(float newTotalTime)
    {
        newTotalTime = Mathf.Max(0f, newTotalTime);

        if (IsCooltimeEnded)
        {
            Time = newTotalTime;
            return;
        }

        float elapsed = ElapsedTime;
        float newRemain = Mathf.Max(0f, newTotalTime - elapsed);

        CancelTimer();
        Time = newTotalTime;

        if (newRemain <= 0f)
        {
            _timeEndCallback?.Invoke();
            return;
        }

        IsCooltimeEnded = false;
        GameManager.instance.timerManager.StartCooltime(
            GetHashCode(),
            _timeEndCallback,
            newRemain,
            elapsed);
    }

    /// <summary>
    /// 기존 경과 시간을 반영하여 타이머를 지정 시간만큼 다시 연장한다.
    /// 전달한 콜백은 연장된 타이머의 새 만료 콜백으로 사용한다.
    /// </summary>
    public void ExtendTimer(float time, Action timerEndCallback = null)
    {
        float elapsedTime = ElapsedTime;
        CancelTimer();
        StartTimer(time + elapsedTime, timerEndCallback);
    }

    /// <summary>
    /// 진행 중인 타이머를 제거하고 종료 상태로 바꾼다.
    /// 자연 만료 콜백은 실행하지 않는다.
    /// </summary>
    public void CancelTimer()
    {
        GameManager.instance.timerManager.CancelTimer(GetHashCode());
        IsCooltimeEnded = true;
    }

    /// <summary>
    /// 다른 Timer의 설정 시간만 복사해 종료 상태로 생성한다.
    /// 실행 중인 예약과 콜백은 복사하지 않는다.
    /// </summary>
    public Timer(Timer cooltime)
    {
        Time = cooltime.Time;
        IsCooltimeEnded = true;
    }

    /// <summary>
    /// 지정한 설정 시간을 가진 종료 상태의 Timer를 생성한다.
    /// StartTimer를 호출하기 전에는 매니저에 등록되지 않는다.
    /// </summary>
    public Timer(float cooltime)
    {
        Time = Mathf.Max(0f, cooltime);
        IsCooltimeEnded = true;
    }

    /// <summary>
    /// 설정 시간이 0인 종료 상태의 Timer를 생성한다.
    /// StartTimer를 호출하기 전에는 매니저에 등록되지 않는다.
    /// </summary>
    public Timer()
    {
        Time = 0f;
        IsCooltimeEnded = true;
    }

}

/// <summary>
/// 등록된 범용 타이머를 매 프레임 스케일 시간으로 갱신한다.
/// 만료 항목은 콜백 전에 제거하여 콜백 내부 재시작을 안전하게 허용한다.
/// </summary>
public class TimerManager : MonoBehaviour
{
    /// <summary>
    /// 한 타이머의 Update 진행 상태와 만료 콜백을 보관한다.
    /// 재계산 시 이전 경과 시간을 오프셋으로 유지할 수 있다.
    /// </summary>
    private sealed class TimerEntry
    {
        public float Duration;
        public float ElapsedTime;
        public float ElapsedOffset;
        public Action EndedCallback;
    }

    /// <summary>
    /// 같은 프레임에 만료한 항목이 콜백으로 매니저를 변경해도 안전하게 처리하기 위한 기록이다.
    /// ID가 재등록되었는지 Entry 참조까지 비교한다.
    /// </summary>
    private readonly struct ExpiredTimer
    {
        public readonly int ID;
        public readonly TimerEntry Entry;

        public ExpiredTimer(int id, TimerEntry entry)
        {
            ID = id;
            Entry = entry;
        }
    }

    [Tooltip("현재 Update에서 진행 중인 타이머를 ID별로 보관한다.")]
    private readonly Dictionary<int, TimerEntry> _cooltimes = new();

    [Tooltip("현재 프레임에 만료한 타이머를 콜백 실행 전까지 임시 보관한다.")]
    private readonly List<ExpiredTimer> _expiredTimers = new();

    /// <summary>
    /// Timer의 설정 시간을 사용하여 새 타이머를 등록한다.
    /// 같은 ID가 이미 존재하면 이전 타이머를 콜백 없이 교체한다.
    /// </summary>
    public void StartCooltime(int id, Timer timer, Action timerEnded)
    {
        StartCooltime(id, timerEnded, timer.Time);
    }

    /// <summary>
    /// 지정한 지속시간으로 새 타이머를 등록한다.
    /// 경과 시간은 0부터 시작하며 Time.deltaTime을 사용한다.
    /// </summary>
    public void StartCooltime(int id, Action timerEnded, float duration)
    {
        StartCooltime(id, timerEnded, duration, 0f);
    }

    /// <summary>
    /// 지정한 지속시간과 기존 경과시간을 가진 타이머를 등록한다.
    /// 총 시간 재계산 뒤에도 ElapsedTime이 되돌아가지 않게 할 때 사용한다.
    /// </summary>
    public void StartCooltime(int id, Action timerEnded, float duration, float elapsedOffset)
    {
        CancelTimer(id);

        _cooltimes[id] = new TimerEntry
        {
            Duration = Mathf.Max(0f, duration),
            ElapsedTime = 0f,
            ElapsedOffset = Mathf.Max(0f, elapsedOffset),
            EndedCallback = timerEnded
        };
    }

    /// <summary>
    /// 등록된 모든 타이머를 스케일 시간으로 진행시키고 만료 콜백을 실행한다.
    /// 콜백 중 같은 ID를 재등록해도 새 타이머가 제거되지 않도록 참조를 확인한다.
    /// </summary>
    private void Update()
    {
        float deltaTime = Time.deltaTime;

        foreach (KeyValuePair<int, TimerEntry> pair in _cooltimes)
        {
            TimerEntry entry = pair.Value;
            entry.ElapsedTime += deltaTime;

            if (entry.ElapsedTime >= entry.Duration)
                _expiredTimers.Add(new ExpiredTimer(pair.Key, entry));
        }

        try
        {
            for (int i = 0; i < _expiredTimers.Count; i++)
            {
                ExpiredTimer expired = _expiredTimers[i];
                if (!_cooltimes.TryGetValue(expired.ID, out TimerEntry current) ||
                    !ReferenceEquals(current, expired.Entry))
                    continue;

                _cooltimes.Remove(expired.ID);
                current.EndedCallback?.Invoke();
            }
        }
        finally
        {
            _expiredTimers.Clear();
        }
    }

    /// <summary>
    /// 지정한 타이머가 지금까지 누적한 전체 경과 시간을 반환한다.
    /// 등록된 타이머가 없으면 0을 반환한다.
    /// </summary>
    public float GetElapsedTime(int id)
    {
        return _cooltimes.TryGetValue(id, out TimerEntry entry)
            ? entry.ElapsedOffset + entry.ElapsedTime
            : 0f;
    }

    /// <summary>
    /// 지정한 ID의 타이머를 만료 콜백 없이 제거한다.
    /// 등록되지 않은 ID라면 아무 작업도 하지 않는다.
    /// </summary>
    public void CancelTimer(int id)
    {
        _cooltimes.Remove(id);
    }
}
