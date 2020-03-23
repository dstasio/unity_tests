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
        RaycastHit Ground, Collision;

        v3 Step = dPos*t + 0.5f*ddPos*t*t;
        if (CheckCollision(Step, out Collision))
        {
            v3 Correction = -(v3.Dot((Step - Collision.distance*Step.normalized), Collision.normal)-0.05f)*Collision.normal;
            //v3 Correction = Step - (Step.normalized*Collision.distance);
            Step += Correction;
        }
        transform.position += Step;
        dPos += ddPos*t;
        CheckDistance = Mathf.Max(v3.Dot((dPos*t + 0.5f*ddPos*t*t), v3.down), 0.1f);
        IsGrounded = CheckGround(CheckDistance, out Ground);
        if (IsGrounded && (dPos.y < 0))
        {
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

    bool CheckCollision(v3 Step, out RaycastHit Hit)
    {
        Hit = new RaycastHit();
        RaycastHit CheckHit = new RaycastHit();
        //bool Collides = Physics.BoxCast(ColliderCenter+transform.position, ColliderRadius, Step.normalized, out Hit, PlayerTransform.rotation, Step.magnitude, 1 << LayerMask.NameToLayer("environment"));
        bool Collides = false;
        v3[] Vertices = new v3[8];
        Vertices[0] = new Vector3( ColliderRadius.x,  ColliderRadius.y,  ColliderRadius.z);
        Vertices[1] = new Vector3( ColliderRadius.x,  ColliderRadius.y, -ColliderRadius.z);
        Vertices[2] = new Vector3( ColliderRadius.x, -ColliderRadius.y,  ColliderRadius.z);
        Vertices[3] = new Vector3( ColliderRadius.x, -ColliderRadius.y, -ColliderRadius.z);
        Vertices[4] = new Vector3(-ColliderRadius.x,  ColliderRadius.y,  ColliderRadius.z);
        Vertices[5] = new Vector3(-ColliderRadius.x,  ColliderRadius.y, -ColliderRadius.z);
        Vertices[6] = new Vector3(-ColliderRadius.x, -ColliderRadius.y,  ColliderRadius.z);
        Vertices[7] = new Vector3(-ColliderRadius.x, -ColliderRadius.y, -ColliderRadius.z);
        
        bool Found = false;
        float FrontFaceDistance = 0;
        for (int i = 0, NChecked = 0; (i < Vertices.Length) && (NChecked < 4); ++i)
        {
            v3 Vertex = Vertices[i];
            Vertex = (PlayerTransform.rotation*Vertex);
            if (v3.Dot(Vertex, Step) > 0)
            {
                Collides = Collides || Physics.Raycast(transform.position+ColliderCenter+Vertex, Step.normalized, out CheckHit, Step.magnitude, 1 << LayerMask.NameToLayer("environment"));
                if (Collides && (!Found || (CheckHit.distance < Hit.distance)))
                {
                    Hit = CheckHit;
                    Found = true;
                }
                FrontFaceDistance += v3.Dot(Vertex, Step.normalized);
                //if (NChecked == 0)
                //{
                //    FrontFaceDistance = v3.Dot(Vertex, Step.normalized);
                //}
                //else
                //{
                //    Debug.Assert(Mathf.Abs(FrontFaceDistance-v3.Dot(Vertex, Step.normalized)) < 0.5f);
                //}
                NChecked++;
            }
        }

        if (!Collides)
        {
            FrontFaceDistance /= 4f;
            Collides = Physics.Raycast(transform.position+ColliderCenter, Step.normalized, out Hit, Step.magnitude+FrontFaceDistance, 1 << LayerMask.NameToLayer("environment"));
            if (Collides)
            {
                Hit.distance -= FrontFaceDistance;
            }
        }

        return(Collides);
    }

    bool CheckGround(float MaxDistance, out RaycastHit Hit)
    {
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
        Matrix4x4 PlayerMatrix = Matrix4x4.Translate(transform.position)*Matrix4x4.Rotate(PlayerTransform.rotation);
        Gizmos.matrix = PlayerMatrix;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(ColliderCenter, ColliderRadius*2);

        //RaycastHit Hit;
        //float MaxDistance = CheckDistance;
        //bool IsHit = Physics.BoxCast(ColliderCenter, ColliderRadius, Vector3.down, out Hit, PlayerTransform.//rotation, MaxDistance, 1 << LayerMask.NameToLayer("environment"));
//
        //Gizmos.color = Color.green;
        //if(IsHit)
        //{
        //    Gizmos.color = Color.red;
        //    Gizmos.DrawWireCube(ColliderCenter + Vector3.down*(Hit.distance), ColliderRadius*2);
        //}
        //Gizmos.DrawRay(ColliderCenter, Vector3.down*(MaxDistance+ColliderRadius.y));

        Gizmos.color = Color.green;
        RaycastHit Collision;
        v3 Step = dPos*Time.fixedDeltaTime + ddPos*Time.fixedDeltaTime*Time.fixedDeltaTime;
        bool Collides = CheckCollision(Step, out Collision);
        Step = PlayerTransform.worldToLocalMatrix*Step;
        if (Collides)
        {
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawRay(Collision.point, Collision.normal);
            Gizmos.matrix = PlayerMatrix;

            Gizmos.DrawWireCube(ColliderCenter + Step.normalized*(Collision.distance), ColliderRadius*2);
        }
        //Gizmos.DrawRay(ColliderCenter, Vector3.down*(MaxDistance+ColliderRadius.y));

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(ColliderCenter+Step, ColliderRadius*2);

    }
}
