using System.Collections.Generic;
using UnityEngine;

public class FootStepModule : InterfaceModule, IFootstep
{
    public float footstepAudioVolume;
    public AudioDataSO DefaultFootstepAudio { get; set; }
    private List<AudioDataSO> footstepAudioList;

    [SerializeField] private DirAnimatableModule dirAnimatableModule;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IFootstep>(this);
    }
    public void Awake()
    {
        if (dirAnimatableModule != null)
            dirAnimatableModule.OnDirChanged += PlayFootstepAudio;
    }
    public void OnDestroy()
    {
        if (dirAnimatableModule != null)
            dirAnimatableModule.OnDirChanged -= PlayFootstepAudio;
    }

    public void Initialize()
    {
        footstepAudioList = new();
    }

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