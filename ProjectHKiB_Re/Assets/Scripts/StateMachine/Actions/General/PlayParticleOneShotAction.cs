using UnityEngine;
[CreateAssetMenu(fileName = "PlayParticleOneShotAction", menuName = "State Machine/Action/General/PlayParticleOneShot")]
public class PlayParticleOneShotAction : StateActionSO
{
    [SerializeField] private ParticlePlayer particlePrefab;
    public override void Act(StateController stateController)
    {
        if (particlePrefab)
            GameManager.instance.particleManager.PlayParticleOneShot(particlePrefab.GetInstanceID(), stateController.transform.position);
    }
}
