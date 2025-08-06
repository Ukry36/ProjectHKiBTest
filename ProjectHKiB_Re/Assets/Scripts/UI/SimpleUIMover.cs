using DG.Tweening;
using UnityEngine;

public class SimpleUIMover : MonoBehaviour
{
    [SerializeField] private Vector2 _shift;
    [SerializeField] private float _delay;
    [SerializeField] private float _duration;
    private Sequence _sequence;
    public void MovePosition()
    {
        Complete();
        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(_delay);
        _sequence.Append(transform.DOLocalMove(_shift, _duration));
        _sequence.Play();
    }

    public void ResetPosition()
    {
        Complete();
        _sequence = DOTween.Sequence();
        _sequence.AppendInterval(_delay);
        _sequence.Append(transform.DOLocalMove(Vector2.zero, _duration));
        _sequence.Play();
    }

    public void TogglePosition(bool set)
    {
        if (set) MovePosition();
        else ResetPosition();
    }

    public void Kill() => _sequence?.Kill();

    public void Complete() => _sequence?.Complete();
}
