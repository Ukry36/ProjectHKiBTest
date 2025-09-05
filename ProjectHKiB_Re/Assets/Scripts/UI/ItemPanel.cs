using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ItemPanel : MonoBehaviour
{
    public Image icon9x9;
    public TextMeshProUGUI itemName;
    public Image itemColor;

    public void SetData(Sprite icon, string name, Color color)
    {
        icon9x9.sprite = icon;
        itemName.text = name;
        itemColor.color = color;
    }
}