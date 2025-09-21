using System;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class MovableModule : InterfaceModule, IMovable
    {
        public float Speed { get; set; }
        public float SprintCoeff { get; set; }
        public LayerMask WallLayer { get; set; }
        public LayerMask CanPushLayer { get; set; }
        public AudioDataSO FootStepAudio { get; set; }
        public float Mass { get; set; }
        [field: SerializeField] public MovePoint MovePoint { get; set; }
        public Vector3 LastSetDir { get; set; }
        public bool IsKnockbackMove { get; set; }
        public bool IsSprinting { get; set; }
        public IMovable.ExternalForce ExForce { get; set; }

        [SerializeField] protected MovementManagerSO movementManager;
        private Coroutine knockBackCoroutine;
        private Action OnKnockBackEnded;

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IMovable>(this);
        }

        public void Initialize()
        {
            MovePoint.Initialize();
            ExForce = new(true);
        }

        public virtual void KnockBack(Vector3 dir, float strength)
        {
            if (strength < Mass) return;
            if (IsKnockbackMove) EndKnockbackEarly();

            IsKnockbackMove = true;
            knockBackCoroutine =
            StartCoroutine(movementManager.KnockBackCoroutine(transform, this, dir, strength, Mass, OnKnockBackEnded));
        }

        public virtual void EndKnockbackEarly()
        {
            if (IsKnockbackMove)
            {
                movementManager.EndKnockbackEarly(transform, this);
                StopCoroutine(knockBackCoroutine);
            }
            KnockBackEndCallback();
        }
        public virtual void KnockBackEndCallback() => IsKnockbackMove = false;
        protected virtual void Awake() => OnKnockBackEnded += KnockBackEndCallback;
        protected virtual void OnDestroy() => OnKnockBackEnded -= KnockBackEndCallback;
    }
}