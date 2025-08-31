using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class SimpleUIMover : MonoBehaviour
{
    public SimpleUIAnimator UIAnimator;
    [SerializeField] private Transform[] _positionPresets;
    [SerializeField] private float _moveDuration;
    [SerializeField] private bool _moveBetweenAnimation;
    private Sequence _moveSequence;
    [SerializeField] private int _currentPosIndex;
    public UnityEvent<bool> OnSetInteractable;
    public UnityEvent OnStartMove;
    public UnityEvent OnEndMove;

    public Transform GetCurrentPos() => _positionPresets[_currentPosIndex];
    public void SetInteractable(bool set)
    {
        OnSetInteractable?.Invoke(set);
    }

    public void Move(int index)
    {
        if (_currentPosIndex == index) return;
        _currentPosIndex = index;
        //transform.position = _enableStartPosition.position;
        SetInteractable(false);
        OnStartMove?.Invoke();
        _moveSequence?.Complete();
        _moveSequence = DOTween.Sequence();
        if (UIAnimator)
        {
            UIAnimator.SetAnimationOut();
            if (_moveBetweenAnimation) _moveSequence.AppendInterval(UIAnimator.GetOutwardDuration());
        }

        _moveSequence.Append(transform.DOMove(_positionPresets[index].position, _moveDuration));
        if (UIAnimator)
        {
            if (_moveBetweenAnimation)
            {
                _moveSequence.AppendCallback(() => UIAnimator.SetAnimationIn());
                _moveSequence.AppendInterval(UIAnimator.GetInwardDuration());
            }
            else
            {
                float inTime = _moveDuration - UIAnimator.GetInwardDuration();
                if (inTime < UIAnimator.GetOutwardDuration()) inTime = UIAnimator.GetOutwardDuration();
                _moveSequence.InsertCallback(inTime, () => UIAnimator.SetAnimationIn());
            }
        }
        _moveSequence.AppendCallback(() => SetInteractable(true));
        _moveSequence.OnComplete(() => OnEndMove?.Invoke());
        _moveSequence.Play();
    }
}
