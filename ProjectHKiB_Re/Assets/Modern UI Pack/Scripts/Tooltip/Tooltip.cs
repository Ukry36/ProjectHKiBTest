using System.Collections;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public RectTransform tooltipRect;
    public RectTransform tooltipContent;
    public RectTransform iconsParent;
    public TextMeshProUGUI descriptionText;
    public LayoutElement contentLE;

    private static readonly WaitForSecondsRealtime _waitForSecondsRealtime0_05 = new(0.05f);

    public void Start()
    {
        if (contentLE == null)
            contentLE = descriptionText.GetComponent<LayoutElement>();
    }
    public void ProcessEnter(TooltipContent tooltip, float preferredWidth)
    {
        descriptionText.text = tooltip.description;
        gameObject.SetActive(true);
        CheckForContentWidth(preferredWidth);

        GetComponent<SimpleUIActivator>().SetEnable();

        if (tooltip.forceToUpdate == true)
            StartCoroutine(nameof(UpdateLayoutPosition));

        Image[] icons = iconsParent.GetComponentsInChildren<Image>();
        for (int i = 0; i < icons.Length; i++)
        {
            if (tooltip.icons.Length > i)
            {
                icons[i].gameObject.SetActive(true);
                icons[i].sprite = tooltip.icons[i];
            }
            else
            {
                icons[i].gameObject.SetActive(false);
            }
        }
    }

    public void ProcessExit()
    {
        GetComponent<SimpleUIActivator>().SetDisable();
    }

    public void CheckForContentWidth(float preferredWidth)
    {
        contentLE.preferredWidth = preferredWidth;
        contentLE.enabled = false;
        float tempWidth = descriptionText.GetComponent<RectTransform>().sizeDelta.x;

        if (tempWidth >= preferredWidth + 1)
            contentLE.enabled = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentLE.GetComponent<RectTransform>());
        contentLE.preferredWidth = preferredWidth;
    }

    IEnumerator UpdateLayoutPosition()
    {
        yield return _waitForSecondsRealtime0_05;
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }
}