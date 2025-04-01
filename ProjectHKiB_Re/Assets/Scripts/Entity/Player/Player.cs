using UnityEngine;
using UnityEngine.U2D.Animation;

public class Player : MonoBehaviour
{
    public PlayerData playerData;

    [SerializeField] private MovementManagerSO movementManager;
    [SerializeField] private GearMergeManagerSO gearMergeManager;
    [SerializeField] private AnimationController animationController;
    [SerializeField] private StateController stateController;
    [SerializeField] private FootstepController footstepController;


    // height based movement test!!!
    [SerializeField] private Transform sprite;
    public float Height
    {
        get => sprite.localPosition.y;
        set
        {
            sprite.localPosition = Vector3.up * value;
            Caninteract = value < canInteractHeight;
        }
    }
    [SerializeField] private float canInteractHeight;
    public bool Caninteract { get; private set; }
    // height based movement test!!!

    [SerializeField] private SpriteLibrary spriteLibrary;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        playerData.Initialize();
        UpdateAnimationController();
        UpdateStateController();
        UpdateFootStepController();
        UpdateSkin();
    }

    public void UpdateSkin()
    {
        playerData.SkinData.SetSKin(spriteLibrary, playerData.AnimatorController, spriteRenderer);
    }


    public void UpdateAnimationController()
    => animationController.animator.runtimeAnimatorController = playerData.AnimatorController;

    public void UpdateStateController()
    {
        stateController.Initialize(playerData.StateMachine);
        stateController.RegisterInterface<IMovable>(playerData);
    }

    public void UpdateFootStepController()
    => footstepController.ChangeDefaultFootStepAudio(playerData.FootStepAudio);

    public void Update()
    {
        movementManager.FollowMovePoint
        (
            transform,
            playerData.MovePoint.transform,
            playerData.Speed.Value * (playerData.IsSprinting ? playerData.SprintCoeff.Value : 1)
        );
    }
}
