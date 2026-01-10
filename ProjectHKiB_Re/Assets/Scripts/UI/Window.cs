using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Window : MonoBehaviour
{
    public bool isPopup;
    public Button initButton;
    [NaughtyAttributes.ReadOnly]public Button lastSelectedButton;

    public enum ButtonSelectMethod { AlwaysInitialize, MaintainWhenPopup, AlwaysMaintain }

    public ButtonSelectMethod buttonSelectMethod;

    public UnityEvent OnWindowShow;
    public UnityEvent OnWindowHide;

    public void SelectInitButton(bool fromPopupClose)
    {
        if (initButton == null) return;
        if (buttonSelectMethod == ButtonSelectMethod.AlwaysMaintain || (buttonSelectMethod == ButtonSelectMethod.MaintainWhenPopup && fromPopupClose))
        {
            if (lastSelectedButton != null && lastSelectedButton.IsActive() && lastSelectedButton.gameObject.activeSelf)
            {
                lastSelectedButton.Select();
                return;
            }
        }
        initButton.Select();
    }

    public void Open() 
    {
        gameObject.SetActive(true);
        OnWindowShow?.Invoke();
    }

    public void Close() 
    {
        gameObject.SetActive(false);
        OnWindowHide?.Invoke();
    }

    public void SetLastSelectedButton(Button button)
    {
        lastSelectedButton = button;
    }
}