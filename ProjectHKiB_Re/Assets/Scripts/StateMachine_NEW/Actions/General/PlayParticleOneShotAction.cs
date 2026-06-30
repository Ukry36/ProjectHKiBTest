using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class PlayParticleOneShotAction : StateAction
    {
        [SerializeField] private ParticlePlayer particlePrefab;
        public override void Act(StateController stateController)
        {
            if (particlePrefab)
                GameManager.instance.particleManager.PlayParticleOneShot(particlePrefab.GetInstanceID(), stateController.transform.position);
        }
    }
}