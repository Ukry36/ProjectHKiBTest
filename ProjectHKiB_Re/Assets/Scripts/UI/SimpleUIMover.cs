using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class SimpleUIMover : MonoBehaviour
{
    public enum MoveMode
    {
        Move, Enable, Disable
    }
    [SerializeField] private Vector2[] _presetLocalPositions;
    [SerializeField] private int _truePositionIndex;
    [SerializeField] private int _falsePositionIndex;
    [SerializeField] private float _delay;
    [SerializeField] private float _duration;
    private Sequence _sequence;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _inAnimationDuration;
    [SerializeField] private float _outAnimationDuration;

    public UnityEvent<bool> OnSetInteractable;

    public void SetInteractable(bool set)
    {
        OnSetInteractable?.Invoke(set);
    }

    public void MovePosition(Vector2 localPos, MoveMode mode)
    {

        SetInteractable(false);
        Complete();
        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(_delay);
        if (_animator != null && (mode == MoveMode.Move || mode == MoveMode.Disable))
        {
            _sequence.AppendCallback(() => _animator.Play("Out"));
            _sequence.AppendInterval(_inAnimationDuration);
            _sequence.AppendCallback(() => _animator.Play(mode == MoveMode.Disable ? "Disabled" : "Moving"));
        }
        _sequence.Append(transform.DOLocalMove(localPos, _duration));
        if (mode == MoveMode.Move || mode == MoveMode.Enable)
        {
            if (_animator != null)
            {
                _sequence.AppendCallback(() => _animator.Play("In"));
                _sequence.AppendInterval(_outAnimationDuration);
                _sequence.AppendCallback(() => _animator.Play("Enabled"));
            }
            _sequence.AppendCallback(() => SetInteractable(true));
        }
        _sequence.Play();
    }
    public void MovePosition(int index, MoveMode mode)
    {
        if (_presetLocalPositions.Length <= index) return;
        MovePosition(_presetLocalPositions[index], mode);
    }
    public void MovePosition() => MovePosition(0, MoveMode.Move);
    public void MovePosition(int index) => MovePosition(index, MoveMode.Move);
    public void ResetPosition() => MovePosition(Vector2.zero, MoveMode.Move);
    public void Enable(Vector2 localPos) => MovePosition(localPos, MoveMode.Enable);
    public void Enable() => Enable(Vector2.zero);
    public void Disable() => MovePosition(transform.localPosition, MoveMode.Disable);
    public void TogglePosition(bool set)
    {
        if (set) MovePosition(_truePositionIndex, MoveMode.Move);
        else MovePosition(_falsePositionIndex, MoveMode.Move);
    }

    public void Kill() => _sequence?.Kill();

    public void Complete() => _sequence?.Complete();
}
