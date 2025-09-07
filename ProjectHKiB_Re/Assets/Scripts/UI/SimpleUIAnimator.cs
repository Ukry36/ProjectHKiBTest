using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class SimpleUIAnimator : MonoBehaviour
{
    public bool useSprite = true;
    [NaughtyAttributes.ShowIf("useSprite")][SerializeField] private Image _image;
    [NaughtyAttributes.ShowIf("useSprite")][SerializeField] private Sprite[] _inwardSpriteSet;
    [NaughtyAttributes.ShowIf("useSprite")][SerializeField] private Sprite[] _outwardSpriteSet;

    [NaughtyAttributes.HideIf("useSprite")][SerializeField] private CanvasGroup[] _inwardObjectSet;
    [NaughtyAttributes.HideIf("useSprite")][SerializeField] private CanvasGroup[] _outwardObjectSet;

    [NaughtyAttributes.MinValue(0)][SerializeField] private float _animInterval;

    private Sequence _animSequence;
    public void SetAnimationIn()
    {
        _animSequence?.Complete();
        _animSequence = DOTween.Sequence();
        int tweenIndex = 0;
        if (useSprite)
        {
            for (int i = 0; i < _inwardSpriteSet.Length; i++)
            {
                _animSequence.AppendCallback(() => _image.sprite = _inwardSpriteSet[tweenIndex++]);
                _animSequence.AppendInterval(_animInterval);
            }
        }
        else
        {
            for (int i = 0; i < _inwardObjectSet.Length; i++)
            {
                _animSequence.AppendCallback(DisableAllAnimObjects);
                _animSequence.AppendCallback(() => _inwardObjectSet[tweenIndex++].alpha = 1);
                _animSequence.AppendInterval(_animInterval);
            }
        }
        _animSequence.Play();
    }
    public void SetAnimationOut()
    {
        _animSequence?.Complete();
        _animSequence = DOTween.Sequence();
        int tweenIndex = 0;
        if (useSprite)
        {
            for (int i = 0; i < _outwardSpriteSet.Length; i++)
            {
                _animSequence.AppendCallback(() => _image.sprite = _outwardSpriteSet[tweenIndex++]);
                _animSequence.AppendInterval(_animInterval);
            }
        }
        else
        {
            for (int i = 0; i < _outwardObjectSet.Length; i++)
            {
                _animSequence.AppendCallback(DisableAllAnimObjects);
                _animSequence.AppendCallback(() => _outwardObjectSet[tweenIndex++].alpha = 1);
                _animSequence.AppendInterval(_animInterval);
            }
        }
        _animSequence.Play();
    }

    private void DisableAllAnimObjects()
    {
        for (int i = 0; i < _inwardObjectSet.Length; i++)
        {
            _inwardObjectSet[i].alpha = 0;
        }
        for (int i = 0; i < _outwardObjectSet.Length; i++)
        {
            _outwardObjectSet[i].alpha = 0;
        }
    }

    public float GetInwardDuration()
    => _animInterval * Mathf.Max(_inwardSpriteSet.Length, _inwardObjectSet.Length);
    public float GetOutwardDuration()
    => _animInterval * Mathf.Max(_outwardSpriteSet.Length, _outwardObjectSet.Length);
}