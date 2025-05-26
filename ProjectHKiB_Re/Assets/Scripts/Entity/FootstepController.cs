using System.Collections.Generic;
using UnityEngine;
public class FootstepController : MonoBehaviour
{
    public float footstepAudioVolume;
    public AudioDataSO DefaultFootstepAudio { get; private set; }
    private List<AudioDataSO> footstepAudioList;

    [SerializeField] private DirAnimationController animationController;

    public void Start()
    {
        footstepAudioList = new();
        if (animationController != null)
            animationController.OnDirChanged += PlayFootstepAudio;
    }

    public void ChangeDefaultFootStepAudio(AudioDataSO audioData)
    => DefaultFootstepAudio = audioData;

    public void PlayFootstepAudio(EnumManager.AnimDir animDir)
    {
        if (DefaultFootstepAudio != null)
            GameManager.instance.audioManager.PlayAudioOneShot(DefaultFootstepAudio, footstepAudioVolume, transform.position);
        if (footstepAudioList.Count > 0)
        {
            for (int i = 0; i < footstepAudioList.Count; i++)
            {
                GameManager.instance.audioManager.PlayAudioOneShot(footstepAudioList[i], footstepAudioVolume, transform.position);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out FloorInfoProvider component))
        {
            footstepAudioList.Add(component.floorInfo.floorAudioData);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.TryGetComponent(out FloorInfoProvider component))
        {
            footstepAudioList.Remove(component.floorInfo.floorAudioData);
        }
    }
}