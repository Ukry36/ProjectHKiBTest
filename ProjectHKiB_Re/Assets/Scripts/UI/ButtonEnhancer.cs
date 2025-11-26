using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonEnhancer : MonoBehaviour
{
    public TextMeshProUGUI TMP;
    [HideInInspector] public Button button;

    public void Awake()
    {
        button = GetComponent<Button>();
    }
}