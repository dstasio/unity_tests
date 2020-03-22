// Copyright (c) Davide Stasio

using UnityEngine;
using UnityEngine.InputSystem;
using v2 = UnityEngine.Vector2;
using v3 = UnityEngine.Vector3;

public class player : MonoBehaviour
{
    struct collider_box
    {
        v3 Center;
        v3 Radius;
    }
    public Animator PlayerAnimator;
    public Transform PlayerTransform;
    //public CharacterController CC;
    public camera Camera;
    public float Sensitivity = 3000.0f;
    public v3 ColliderCenter;
    public v3 ColliderRadius;

    game_controls Controls;

    v3 Force;
    v3 ddPos;
    v3 dPos;
    public float Mass = 70;
    public float FrictionCoefficientGround = 0.9f;
    public float JumpForce = 13000f;
    public float Gravity = 26f;
    bool IsGrounded = true;
    float Speed;

    void Awake()
    {
        Controls = new game_controls();
        Controls.InGame.RotateCamera.performed += ctx => Camera.Input = ctx.ReadValue<Vector2>();
        Controls.InGame.RotateCamera.canceled += _ => Camera.Input = Vector2.zero;

        Controls.InGame.Move.performed += ctx => SetForce(ctx.ReadValue<Vector2>());
        Controls.InGame.Move.canceled += _ => Force = Vector2.zero;

        Controls.InGame.Jump.performed += _ => { Force.y = IsGrounded ? JumpForce : 0; };
        Controls.InGame.Jump.canceled += _ => Force.y = 0;

        PlayerAnimator = GetComponentInChildren<Animator>();
    }
    
    void Move(v3 Delta)
    {
        transform.position += Delta;
    }

    void SetForce(v2 Input)
    {
        v3 Right = Camera.transform.right;
        Right.y = 0;
        Right.Normalize();
        v3 Forward = Camera.transform.forward;
        Forward.y = 0;
        Forward.Normalize();
        v3 Direction = Input.x*Right + Input.y*Forward;
        Force.x = Direction.x*Sensitivity;
        Force.z = Direction.z*Sensitivity;

        PlayerTransform.LookAt(PlayerTransform.position + Direction);
    }

    void Update()
    {
        float Weight = Mass * 9.81f;
        float FrictionCoefficient = FrictionCoefficientGround;
        v3 Friction = -dPos * Weight * FrictionCoefficient;
        if (IsGrounded)
        {
            Friction.y = 0;
        }
        else
        {
            Friction.y = -Mass*Gravity;
            Force.y = 0;
        }
        v3 TotalForce = Force + Friction;
        ddPos = TotalForce / Mass;
    }

    float CheckDistance;
    void FixedUpdate()
    {
        float t = Time.fixedDeltaTime;
        RaycastHit Ground;

        transform.position += dPos*t + 0.5f*ddPos*t*t;
        dPos += ddPos*t;
        CheckDistance = Mathf.Max(v3.Dot((dPos*t + 0.5f*ddPos*t*t), v3.down), 0.1f);
        IsGrounded = CheckGround(CheckDistance, out Ground);
        if (IsGrounded && (dPos.y < 0))
        {
            Debug.Log(Ground.distance);
            dPos.y = Ground.distance > 0.01f ? -(Ground.distance-0.005f)/t : 0;
            ddPos.y = 0;
        }

        Speed = dPos.magnitude;
        PlayerAnimator.SetFloat("Speed", Speed);

        if (Speed < 0.01)
        {
            dPos = Vector2.zero;
        }
    }

    bool CheckGround(float MaxDistance, out RaycastHit Hit)
    {
        // TODO(dave): MaxDistance based on current speed
        bool IsHit = Physics.BoxCast(ColliderCenter+transform.position, ColliderRadius, Vector3.down, out Hit, PlayerTransform.rotation, MaxDistance, 1 << LayerMask.NameToLayer("environment"));

        return(IsHit);
    }
    bool CheckGround(float MaxDistance)
    {
        RaycastHit Hit;
        bool IsHit = CheckGround(MaxDistance, out Hit);
        return(IsHit);
    }

    private void OnEnable() {
        Controls.Enable();
    }

    private void OnDisable() {
        Controls.Disable();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + ColliderCenter, ColliderRadius*2);

        RaycastHit Hit;
        float MaxDistance = CheckDistance;
        bool IsHit = Physics.BoxCast(ColliderCenter+transform.position, ColliderRadius, Vector3.down, out Hit, PlayerTransform.rotation, MaxDistance, 1 << LayerMask.NameToLayer("environment"));

        Gizmos.color = Color.green;
        if(IsHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position+ColliderCenter + Vector3.down*(Hit.distance), ColliderRadius*2);
        }
        Gizmos.DrawRay(ColliderCenter+transform.position, Vector3.down*(MaxDistance+ColliderRadius.y));

        Gizmos.color = Color.blue;
        v3 Velocity = dPos*Time.fixedDeltaTime + 0.5f*ddPos*Time.fixedDeltaTime*Time.fixedDeltaTime;
        Gizmos.DrawWireCube(transform.position+ColliderCenter+Velocity, ColliderRadius*2);
    }
}
