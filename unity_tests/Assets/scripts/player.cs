// Copyright (c) Davide Stasio

using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    public Animator PlayerAnimator;
    public Transform PlayerTransform;
    public CharacterController CC;
    public camera Camera;
    public float Sensitivity = 500.0f;

    game_controls Controls;

    Vector3 Force;
    Vector3 ddPos;
    Vector3 dPos;
    Vector3 LastPos;
    public float Mass = 70;
    public float FrictionCoefficientGround = 0.9f;
    public float FrictionCoefficientAir = 0.3f;
    public float Speed;
    public float JumpForce = 30f;
    public float Gravity = 9.81f;
    bool IsGrounded = true;

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
        CC = GetComponent<CharacterController>();
    }
    
    void Move(Vector3 Delta)
    {
        transform.position += Delta;
    }

    void SetForce(Vector2 Input)
    {
        Vector3 Right = Camera.transform.right;
        Right.y = 0;
        Right.Normalize();
        Vector3 Forward = Camera.transform.forward;
        Forward.y = 0;
        Forward.Normalize();
        Vector3 Direction = Input.x*Right + Input.y*Forward;
        Force.x = Direction.x*Sensitivity;
        Force.z = Direction.z*Sensitivity;

        PlayerTransform.LookAt(PlayerTransform.position + Direction);
    }

    void Update()
    {
        float Weight = Mass * 9.81f;
        float FrictionCoefficient = FrictionCoefficientGround;
        Vector3 Friction = -dPos * Weight * FrictionCoefficient;
        if (IsGrounded)
        {
            Friction.y = 0;
        }
        else
        {
            Friction.y = -Mass*Gravity;
            Force.y = 0;
        }
        Vector3 TotalForce = Force + Friction;
        ddPos = TotalForce / Mass;
    }

    void FixedUpdate()
    {
        float t = Time.fixedDeltaTime;
        LastPos = transform.position;
        CC.Move(dPos*t + 0.5f*ddPos*t*t);
        dPos += ddPos*t;
        IsGrounded = CheckGround();
        if (IsGrounded)
        {
            dPos.y = 0;
            ddPos.y = 0;
        }

        Speed = dPos.magnitude;
        PlayerAnimator.SetFloat("Speed", Speed);

        if (Speed < 0.01)
        {
            dPos = Vector2.zero;
        }
    }

    bool CheckGround()
    {
        RaycastHit Hit;
        float MaxDistance = 0.1f;
        bool IsHit = Physics.BoxCast(CC.center+transform.position, new Vector3(CC.radius, CC.height/2, CC.radius), Vector3.down, out Hit, PlayerTransform.rotation, MaxDistance, 1 << LayerMask.NameToLayer("environment"));

        if(IsHit)
        {
            Debug.Log("Grounded");
        }

        return(IsHit);
    }

    private void OnEnable() {
        Controls.Enable();
    }

    private void OnDisable() {
        Controls.Disable();
    }

    private void OnDrawGizmos() {
        RaycastHit Hit;
        float MaxDistance = 0.03f;
        bool IsHit = Physics.BoxCast(CC.center+transform.position, new Vector3(CC.radius, CC.height/2, CC.radius), Vector3.down, out Hit, PlayerTransform.rotation, MaxDistance, 1 << LayerMask.NameToLayer("environment"));

        Gizmos.color = Color.green;
        if(IsHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(CC.center+transform.position + Vector3.down*(Hit.distance), new Vector3(CC.radius*2, CC.height, CC.radius*2));
        }
        Gizmos.DrawRay(CC.center+transform.position, Vector3.down*(MaxDistance+CC.height/2));
    }
}
