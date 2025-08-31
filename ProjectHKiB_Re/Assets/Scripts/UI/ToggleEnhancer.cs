using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(UnityEngine.UI.Toggle))]
public class ToggleEnhancer : MonoBehaviour
{
    public UnityEvent OnValueOn;
    public UnityEvent OnValueOff;

    public void OnValueChanged(bool value)
    {
        if (value) OnValueOn?.Invoke();
        else OnValueOff?.Invoke();
    }

    private void Start()
    {
        GetComponent<UnityEngine.UI.Toggle>().onValueChanged.AddListener(OnValueChanged);
    }
}