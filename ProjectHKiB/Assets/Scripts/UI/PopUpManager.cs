using System;
using System.Diagnostics.Tracing;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PopUpManager : MonoBehaviour
{
    public UnityEvent onPopUpConfirmed;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public GameObject popUpUI;

    public void Initialize(string _title, string _descirption, UnityAction onPopUpConfirmedListner)
    {
        title.text = _title;
        description.text = _descirption;
        onPopUpConfirmed.AddListener(onPopUpConfirmedListner);
    }

    public void PopUp()
    {
        // show ui!
    }

    public void OnConfirmed()
    {
        popUpUI.SetActive(true);
        onPopUpConfirmed.Invoke();
    }

    public void OnDenied()
    {
        popUpUI.SetActive(false);
    }
}