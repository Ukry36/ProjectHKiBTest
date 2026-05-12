using UnityEngine;

public class BuffEventTestModule : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _defaultTarget;
    [SerializeField] private bool _useSelfIfTargetIsNull = true;

    [Header("First Emotion")]
    [SerializeField] private KeyCode _firstKey = KeyCode.B;
    [SerializeField] private EmotionColor _firstColor = EmotionColor.Sadness;
    [SerializeField] private int _firstStack = 1;

    [Header("Second Emotion")]
    [SerializeField] private KeyCode _secondKey = KeyCode.N;
    [SerializeField] private EmotionColor _secondColor = EmotionColor.Happiness;
    [SerializeField] private int _secondStack = 1;

    [Header("Option")]
    [SerializeField] private float _overrideDuration = -1f;
    [SerializeField] private bool _showLog = true;

    private void Update()
    {
        if (Input.GetKeyDown(_firstKey))
            ApplyEmotionToDefaultTarget(_firstColor, _firstStack);

        if (Input.GetKeyDown(_secondKey))
            ApplyEmotionToDefaultTarget(_secondColor, _secondStack);
    }

    private void ApplyEmotionToDefaultTarget(EmotionColor color, int stack)
    {
        Transform target = _defaultTarget;

        if (target == null && _useSelfIfTargetIsNull)
            target = transform;

        ApplyEmotion(target, color, stack);
    }

    public void ApplyEmotion(Transform target, EmotionColor color, int stack)
    {
        if (target == null)
        {
            Debug.LogError("[BuffEventTestModule] Target이 null임.");
            return;
        }

        IEmotionModule emotionModule = FindEmotionModule(target);

        if (emotionModule == null)
        {
            Debug.LogError($"[BuffEventTestModule] {target.name} 에서 IEmotionModule을 찾지 못함.");
            return;
        }

        emotionModule.ApplyColor(color, stack, _overrideDuration);

        if (_showLog)
        {
            Debug.Log(
                $"[BuffEventTestModule] {target.name} 에게 {color} {stack}스택 적용"
            );
        }
    }

    private IEmotionModule FindEmotionModule(Transform target)
    {
        if (target.TryGetComponent(out InterfaceRegister register) &&
            register.TryGetInterface(out IEmotionModule emotionModule))
        {
            return emotionModule;
        }

        InterfaceRegister parentRegister = target.GetComponentInParent<InterfaceRegister>();
        if (parentRegister != null &&
            parentRegister.TryGetInterface(out emotionModule))
        {
            return emotionModule;
        }

        InterfaceRegister childRegister = target.GetComponentInChildren<InterfaceRegister>();
        if (childRegister != null &&
            childRegister.TryGetInterface(out emotionModule))
        {
            return emotionModule;
        }

        EmotionModule directModule = target.GetComponent<EmotionModule>();
        if (directModule != null)
            return directModule;

        EmotionModule parentModule = target.GetComponentInParent<EmotionModule>();
        if (parentModule != null)
            return parentModule;

        EmotionModule childModule = target.GetComponentInChildren<EmotionModule>();
        if (childModule != null)
            return childModule;

        return null;
    }
}