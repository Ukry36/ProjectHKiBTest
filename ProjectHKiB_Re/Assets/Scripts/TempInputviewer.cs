using TMPro;
using UnityEngine;

public class TempInputviewer : MonoBehaviour
{
    public TextMeshProUGUI tmp;

    void Update()
    {
        tmp.text = GameManager.instance.inputManager.MoveInput.ToString();
    }
}
