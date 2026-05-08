using System;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class MovableModule : InterfaceModule, IMovable
    {
        public Vector3 Velocity { get; set; }
        public float WalkSpeed { get; set; }
        public bool IsWalking { get; set; }
    public Vector2 WalkingDir { get; set; }
        public float SprintCoeff { get; set; }
        public LayerMask WallLayer { get; set; }
        public LayerMask CanPushLayer { get; set; }
        public AudioDataSO FootStepAudio { get; set; }
        public float Mass { get; set; }
        [field: SerializeField] public MovePoint MovePoint { get; set; }
        public Vector3 LastSetDir { get; set; }
        public bool IsKnockbackMove { get; set; }
        public bool IsSprinting { get; set; }
        public ExternalForce ExForce { get; set; }
        [field: SerializeField] public BodyComponent[] BodyComponents { get; set; }

        public float ZPosition 
        { 
            get => transform.position.z; 
            set { SetBodyPartZLevel(value);}
        }

        [NaughtyAttributes.Button]public void ZPlus1() => ZPosition += 1;
        [NaughtyAttributes.Button]public void ZMinus1() => ZPosition -= 1;

        //[SerializeField] protected MovementManagerSO movementManager;
        private Coroutine knockBackCoroutine;
        private Action OnKnockBackEnded;

        private void SetBodyPartZLevel(float z)
        {
            float d = z - transform.position.z;
            for(int i = 0; i < BodyComponents.Length; i++) BodyComponents[i].SetZ(z, d);
            MovePoint.transform.position += Vector3.forward * d;
            transform.position += Vector3.forward * d;
        }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IMovable>(this);
        }

        public void Initialize()
        {
            MovePoint.Initialize(this);
            ExForce = new();
        }

        public virtual void KnockBack(Vector3 dir, float strength)
        {
            if (strength < Mass) return;
            if (IsKnockbackMove) EndKnockbackEarly();

            IsKnockbackMove = true;
            Debug.LogError("ERROR: Not Implemented!!!");
            //knockBackCoroutine = StartCoroutine(movementManager.KnockBackCoroutine(transform, this, dir, strength, Mass, OnKnockBackEnded));
        }

        public virtual void EndKnockbackEarly()
        {
            if (IsKnockbackMove)
            {
                Debug.LogError("ERROR: Not Implemented!!!");
                //movementManager.EndKnockbackEarly(transform, this);
                StopCoroutine(knockBackCoroutine);
            }
            KnockBackEndCallback();
        }
        public virtual void KnockBackEndCallback() => IsKnockbackMove = false;
        protected virtual void Awake() => OnKnockBackEnded += KnockBackEndCallback;
        protected virtual void OnDestroy() => OnKnockBackEnded -= KnockBackEndCallback;
    }
}