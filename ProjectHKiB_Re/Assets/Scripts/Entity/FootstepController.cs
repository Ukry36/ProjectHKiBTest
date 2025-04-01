using System.Collections.Generic;
using UnityEngine;
public class FootstepController : MonoBehaviour
{
    public float footstepAudioVolume;
    public AudioDataSO DefaultFootstepAudio { get; private set; }
    private List<AudioDataSO> footstepAudioList;
    [SerializeField] private CollisionManagerSO collisionManager;

    [SerializeField] private AnimationController animationController;

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
            AudioManager.instance.PlayAudioOneShot(DefaultFootstepAudio, footstepAudioVolume, transform.position);
        if (footstepAudioList.Count > 0)
        {
            for (int i = 0; i < footstepAudioList.Count; i++)
            {
                AudioManager.instance.PlayAudioOneShot(footstepAudioList[i], footstepAudioVolume, transform.position);
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