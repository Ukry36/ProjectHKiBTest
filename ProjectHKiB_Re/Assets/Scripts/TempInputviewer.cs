using TMPro;
using UnityEngine;

public class TempInputviewer : MonoBehaviour
{
    public TextMeshProUGUI tmp;

    void Update()
    {
        tmp.text = "FPS: " + Time.captureFramerate;
        //tmp.text = GameManager.instance.inputManager.MoveInput.ToString();
        //tmp.text = "ATK: " + GameManager.instance.player.StateController.GetInterface<IAttackable>().ATK.ToString();
    }
}
