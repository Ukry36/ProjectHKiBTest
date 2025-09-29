using System;
using Assets.Scripts.Interfaces.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
public class GuageManager : MonoBehaviour
{
    public Image guageOutline;
    public Image guage;
    public TextMeshProUGUI text;
    public DamagableModule target;

    // in another script
    [SerializeField] private float _hp;
    [SerializeField] private float _maxHp;
    public ReactiveProperty<Vector2> hpReactiveProperty;
    [NaughtyAttributes.Button]
    public void Plus10()
    {
        hpReactiveProperty.Value += 10 * Vector2.right;
        if (hpReactiveProperty.Value.y < hpReactiveProperty.Value.x)
            hpReactiveProperty.Value = hpReactiveProperty.Value.y * Vector2.one;
    }
    [NaughtyAttributes.Button]
    public void Minus10()
    {
        hpReactiveProperty.Value -= 10 * Vector2.right;
        if (hpReactiveProperty.Value.x < 0)
            hpReactiveProperty.Value = hpReactiveProperty.Value.y * Vector2.up;
    }
    [NaughtyAttributes.Button]
    public void PlusMax10() => hpReactiveProperty.Value += 10 * Vector2.up;
    [NaughtyAttributes.Button]
    public void MinusMax10()
    {
        hpReactiveProperty.Value -= 10 * Vector2.up;
        if (hpReactiveProperty.Value.y < hpReactiveProperty.Value.x)
            hpReactiveProperty.Value = hpReactiveProperty.Value.y * Vector2.one;
        if (hpReactiveProperty.Value.y < 0)
            hpReactiveProperty.Value = Vector2.zero;
    }

    public void Awake()
    {
        hpReactiveProperty = new(new(_hp, _maxHp));
        hpReactiveProperty.Subscribe(hp => { _hp = hp.x; _maxHp = hp.y; });
    }
    public void OnDestroy()
    {
        hpReactiveProperty?.Dispose();
    }
    // in another script

    public void Start()
    {
        hpReactiveProperty.Subscribe(hp => { guage.fillAmount = hp.x / hp.y; text.text = hp.x.ToString(); });
    }


}