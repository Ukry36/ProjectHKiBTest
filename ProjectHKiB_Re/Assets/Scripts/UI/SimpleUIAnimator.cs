using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class SimpleUIAnimator : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private Sprite[] _inwardSpriteSet;
    [SerializeField] private Sprite[] _outwardSpriteSet;
    [NaughtyAttributes.MinValue(0)][SerializeField] private float _animInterval;

    private Sequence _animSequence;
    public void SetAnimationIn()
    {
        _animSequence?.Complete();
        _animSequence = DOTween.Sequence();
        int tweenIndex = 0;
        for (int i = 0; i < _inwardSpriteSet.Length; i++)
        {
            _animSequence.AppendCallback(() => _image.sprite = _inwardSpriteSet[tweenIndex++]);
            _animSequence.AppendInterval(_animInterval);
        }
        _animSequence.Play();
    }

    public void SetAnimationOut()
    {
        _animSequence?.Complete();
        _animSequence = DOTween.Sequence();
        int tweenIndex = 0;
        for (int i = 0; i < _outwardSpriteSet.Length; i++)
        {
            _animSequence.AppendCallback(() => _image.sprite = _outwardSpriteSet[tweenIndex++]);
            _animSequence.AppendInterval(_animInterval);
        }
        _animSequence.Play();
    }

    public float GetInwardDuration() => _animInterval * _inwardSpriteSet.Length;

    public float GetOutwardDuration() => _animInterval * _outwardSpriteSet.Length;

}