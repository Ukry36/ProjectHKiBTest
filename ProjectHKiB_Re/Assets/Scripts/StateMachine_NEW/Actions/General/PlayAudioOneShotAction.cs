using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class PlayAudioOneShotAction : StateAction
    {
        [SerializeField] private AudioDataSO audioData;
        [Range(0, 1)][SerializeField] private float volume;
        public override void Act(StateController stateController)
        {
            if (audioData)
                GameManager.instance.audioManager.PlayAudioOneShot(audioData, volume, stateController.transform.position);
        }
    }
}