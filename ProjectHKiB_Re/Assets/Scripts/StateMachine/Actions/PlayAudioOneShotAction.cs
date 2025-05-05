using UnityEngine;
[CreateAssetMenu(fileName = "PlayAudioOneShotAction", menuName = "Scriptable Objects/State Machine/Action/PlayAudioOneShot", order = 3)]
public class PlayAudioOneShotAction : StateActionSO
{
    [SerializeField] private AudioDataSO audioData;
    [Range(0, 1)][SerializeField] private float volume;
    public override void Act(StateController stateController)
    {
        if (audioData)
            GameManager.instance.audioManager.PlayAudioOneShot(audioData, volume, stateController.transform.position);
    }
}
