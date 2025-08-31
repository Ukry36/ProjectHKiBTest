using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class SimpleUIActivator : MonoBehaviour
{
    public SimpleUIAnimator UIAnimator;
    public bool UIEnabled;
    public bool useMove;
    public bool useUIMoverAsEnablePos;
    private bool TEMP() => useMove && !useUIMoverAsEnablePos;
    [NaughtyAttributes.ShowIf("useMove")][SerializeField] private bool _moveAfterAnimation;
    [NaughtyAttributes.ShowIf("useMove")][SerializeField] private Transform _enableStartPosition;
    [NaughtyAttributes.ShowIf("useUIMoverAsEnablePos")][SerializeField] private SimpleUIMover UIMover;
    [NaughtyAttributes.ShowIf("TEMP")][SerializeField] private Transform _enabledPosition;
    [NaughtyAttributes.ShowIf("useMove")][SerializeField] private Transform _disableStartPosition;
    [NaughtyAttributes.ShowIf("useMove")][SerializeField] private Transform _disabledPosition;
    [NaughtyAttributes.ShowIf("useMove")][NaughtyAttributes.MinValue(0)][SerializeField] private float _moveDuration;

    private Sequence _moveSequence;
    private Sequence _setActiveSequence;

    public UnityEvent<bool> OnSetInteractable;
    public UnityEvent OnStartEnable;
    public UnityEvent OnEndDisable;

    public void SetInteractable(bool set)
    {
        OnSetInteractable?.Invoke(set);
    }


    private void SetMoveIn()
    {
        if (!useMove) return;
        transform.position = _enableStartPosition.position;
        _moveSequence?.Complete();
        _moveSequence = DOTween.Sequence();
        if (_moveAfterAnimation && UIAnimator) _moveSequence.AppendInterval(UIAnimator.GetInwardDuration());
        if (useUIMoverAsEnablePos)
            _moveSequence.Append(transform.DOMove(UIMover.GetCurrentPos().position, _moveDuration));
        else
            _moveSequence.Append(transform.DOMove(_enabledPosition.position, _moveDuration));
        _moveSequence.Play();
    }

    private void SetMoveOut()
    {
        if (!useMove) return;
        transform.position = _disableStartPosition.position;
        _moveSequence?.Complete();
        _moveSequence = DOTween.Sequence();
        if (_moveAfterAnimation && UIAnimator) _moveSequence.AppendInterval(UIAnimator.GetOutwardDuration());
        _moveSequence.Append(transform.DOMove(_disabledPosition.position, _moveDuration));
        _moveSequence.Play();
    }

    private void SetActiveTrue()
    {
        SetInteractable(false);
        if (!useMove) _moveDuration = 0;
        float interval = _moveDuration;
        if (UIAnimator) interval = UIAnimator.GetInwardDuration() > _moveDuration ? UIAnimator.GetInwardDuration() : _moveDuration;
        gameObject.SetActive(true);
        OnStartEnable?.Invoke();
        _setActiveSequence?.Complete();
        _setActiveSequence = DOTween.Sequence();
        _setActiveSequence.AppendInterval(interval);
        _setActiveSequence.OnComplete(() => SetInteractable(true));
        _setActiveSequence.Play();
    }

    private void SetActiveFalse()
    {
        SetInteractable(false);
        if (!useMove) _moveDuration = 0;
        float interval = _moveDuration;
        if (UIAnimator) interval = UIAnimator.GetOutwardDuration() > _moveDuration ? UIAnimator.GetOutwardDuration() : _moveDuration;
        _setActiveSequence?.Complete();
        _setActiveSequence = DOTween.Sequence();
        _setActiveSequence.AppendInterval(interval);
        _setActiveSequence.AppendCallback(() => gameObject.SetActive(false));
        _setActiveSequence.AppendCallback(() => OnEndDisable?.Invoke());
        _setActiveSequence.OnComplete(() => SetInteractable(true));
        _setActiveSequence.Play();
    }

    [NaughtyAttributes.Button()]
    public void SetEnable()
    {
        if (UIEnabled) return;
        UIEnabled = true;
        if (UIAnimator) UIAnimator.SetAnimationIn();
        SetMoveIn();
        SetActiveTrue();
    }
    [NaughtyAttributes.Button()]
    public void SetDisable()
    {
        if (!UIEnabled) return;
        UIEnabled = false;
        if (UIAnimator) UIAnimator.SetAnimationOut();
        SetMoveOut();
        SetActiveFalse();
    }

    public void InstantSetEnable()
    {
        if (UIEnabled) return;
        UIEnabled = true;
        gameObject.SetActive(true);
        OnStartEnable?.Invoke();
    }
    public void InstantSetDisable()
    {
        if (!UIEnabled) return;
        UIEnabled = false;
        gameObject.SetActive(false);
        OnEndDisable?.Invoke();
    }

    public void SetUIActive(bool active)
    {
        if (active) SetEnable();
        else SetDisable();
    }
}