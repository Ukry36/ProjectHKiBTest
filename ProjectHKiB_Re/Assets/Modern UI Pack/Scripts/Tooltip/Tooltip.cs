using System.Collections;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public RectTransform tooltipContent;
    public RectTransform ImagesParent;
    public TextMeshProUGUI descriptionText;
    public float preferredWidth;

    public void ProcessEnter(TooltipContent tooltip)
    {
        descriptionText.text = tooltip.description;
        gameObject.SetActive(true);
        CheckForContentWidth();

        GetComponent<SimpleUIActivator>().SetEnable();

        Image[] images = ImagesParent.GetComponentsInChildren<Image>(true);

        images[0].gameObject.SetActive(tooltip.image36x36);
        if (tooltip.image36x36) images[0].sprite = tooltip.image36x36;

        images[1].gameObject.SetActive(tooltip.icon9x9);
        if (tooltip.icon9x9) images[1].sprite = tooltip.icon9x9;

        for (int i = 2; i < images.Length; i++)
        {
            if (tooltip.icon5x5s.Length + 2 > i && images[i] != null)
            {
                images[i].gameObject.SetActive(true);
                images[i].sprite = tooltip.icon5x5s[i - 2];
            }
            else
            {
                images[i].gameObject.SetActive(false);
            }
        }
    }

    public void ProcessExit()
    {
        GetComponent<SimpleUIActivator>().SetDisable();
    }

    public void CheckForContentWidth()
    {
        LayoutElement contentLE = descriptionText.GetComponent<LayoutElement>();
        contentLE.preferredWidth = preferredWidth;
        contentLE.enabled = false;
        float tempWidth = descriptionText.GetComponent<RectTransform>().sizeDelta.x;

        if (tempWidth >= preferredWidth + 1)
            contentLE.enabled = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentLE.GetComponent<RectTransform>());
        contentLE.preferredWidth = preferredWidth;
    }
}