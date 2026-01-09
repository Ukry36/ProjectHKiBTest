using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ButtonEnhanced : Button, ISelectHandler, IDeselectHandler
{
    [FormerlySerializedAs("onSelected")]
    [SerializeField]
    private UnityEvent m_OnSelect = new();
    [FormerlySerializedAs("onDeselected")]
    [SerializeField]
    private UnityEvent m_OnDeselect = new();

    public TextMeshProUGUI text;
    public TextMeshProUGUI number;

    public bool overrideNavigation;

    private void SelectButton()
    {
        if (!IsActive() || !IsInteractable())return;
        m_OnSelect.Invoke();
        DoStateTransition(SelectionState.Selected, false);
    }

    private void DeselectButton()
    {
        if (!IsActive() || !IsInteractable())return;
        m_OnDeselect.Invoke();
        DoStateTransition(SelectionState.Normal, false);
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        SelectButton();
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        DeselectButton();
    }

    public void ObtainSelectionPoint()
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
        m_OnSelect.Invoke();
    }

    private List<Transform> _selectables = new();

    private int GetSiblingNumAndSetActiveSelectables(Transform child)
    {
        Transform parent = child.parent;
        int siblingNum = 0;
        _selectables.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).gameObject.activeSelf)
            {
                _selectables.Add(parent.GetChild(i));
                if (child == parent.GetChild(i)) 
                    siblingNum = _selectables.Count - 1;
            }
        }
        return siblingNum;
    }

    private int BoundIndex(int index)
    {
        if (index >= _selectables.Count) return BoundIndex(index - _selectables.Count);
        else if (index < 0) return BoundIndex(_selectables.Count + index);
        else return index;
    }

    // this codes temporarily manages only when parent GridLayoutGroup setting is UpperLeft / Horizontal / UpperLeft / not Flexible
    public override Selectable FindSelectableOnUp() 
    {
        if (overrideNavigation && transform.parent.TryGetComponent(out GridLayoutGroup layout))
        {
            if (layout.constraint != GridLayoutGroup.Constraint.Flexible)
            {
                int siblingNum = GetSiblingNumAndSetActiveSelectables(transform);
                Transform nextSelectable = _selectables[BoundIndex(siblingNum - layout.constraintCount)];
                return nextSelectable.GetComponent<Selectable>();
            }
        }

        return base.FindSelectableOnUp();
    }
    public override Selectable FindSelectableOnDown() 
    {
        if (overrideNavigation && transform.parent.TryGetComponent(out GridLayoutGroup layout))
        {
            if (layout.constraint != GridLayoutGroup.Constraint.Flexible)
            {
                int siblingNum = GetSiblingNumAndSetActiveSelectables(transform);
                Transform nextSelectable = _selectables[BoundIndex(siblingNum + layout.constraintCount)];
                return nextSelectable.GetComponent<Selectable>();
            }
        }

        return base.FindSelectableOnDown();
    }
    public override Selectable FindSelectableOnLeft() 
    {
        if (overrideNavigation && transform.parent.TryGetComponent(out GridLayoutGroup layout))
        {
            if (layout.constraint != GridLayoutGroup.Constraint.Flexible)
            {
                int siblingNum = GetSiblingNumAndSetActiveSelectables(transform);
                Transform nextSelectable = _selectables[BoundIndex(siblingNum - 1)];
                return nextSelectable.GetComponent<Selectable>();
            }
        }

        return base.FindSelectableOnLeft();
    }
    public override Selectable FindSelectableOnRight() 
    {
        if (overrideNavigation && transform.parent.TryGetComponent(out GridLayoutGroup layout))
        {
            if (layout.constraint != GridLayoutGroup.Constraint.Flexible)
            {
                int siblingNum = GetSiblingNumAndSetActiveSelectables(transform);
                Transform nextSelectable = _selectables[BoundIndex(siblingNum + 1)];
                return nextSelectable.GetComponent<Selectable>();
            }
        }

        return base.FindSelectableOnRight();
    }

    public override void OnMove(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Right:
                Navigate(eventData, FindSelectableOnRight());
                break;

            case MoveDirection.Up:
                Navigate(eventData, FindSelectableOnUp());
                break;

            case MoveDirection.Left:
                Navigate(eventData, FindSelectableOnLeft());
                break;

            case MoveDirection.Down:
                Navigate(eventData, FindSelectableOnDown());
                break;
        }
    }

    void Navigate(AxisEventData eventData, Selectable sel)
    {
        if (sel != null && sel.IsActive())
            eventData.selectedObject = sel.gameObject;
    }
}