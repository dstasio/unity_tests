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
    private CharacterController controller;

    game_controls Controls;

    v3 Force;
    v3 ddPos;
    v3 dPos;
    public float Mass = 70;
    public float friction_coefficient_ground = 0.9f;
    public float JumpForce = 13000f;
    public float Gravity = 26f;
    bool is_grounded = true;
    float Speed;

    void Awake()
    {
        Controls = new game_controls();
        Controls.InGame.RotateCamera.performed += ctx => Camera.Input = ctx.ReadValue<Vector2>();
        Controls.InGame.RotateCamera.canceled += _ => Camera.Input = Vector2.zero;

        Controls.InGame.Move.performed += ctx => set_force(ctx.ReadValue<Vector2>());
        Controls.InGame.Move.canceled += _ => Force = Vector2.zero;

        Controls.InGame.Jump.performed += _ => { Force.y = is_grounded ? JumpForce : 0; };
        Controls.InGame.Jump.canceled += _ => Force.y = 0;

        PlayerAnimator = GetComponentInChildren<Animator>();

        controller = GetComponent<CharacterController>();
    }
    
    void set_force(v2 Input)
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
        float weight = Mass * 9.81f;
        float friction_coefficient = friction_coefficient_ground;
        v3 friction = -dPos * weight * friction_coefficient;
        if (is_grounded)
        {
            friction.y = 0;
            friction.y = -Mass*Gravity;
        }
        else
        {
            friction.y = -Mass*Gravity;
            Force.y = 0;
        }
        ddPos = (Force + friction) / Mass;

        float t = Time.deltaTime;
        float ground_check_distance;
        RaycastHit ground;

        v3 step = dPos*t + 0.5f*ddPos*t*t;
        dPos += ddPos*t;
        // @note: in a normal engine, collision calculations would go here!
        Debug.DrawLine(transform.position, transform.position+step*50f, Color.blue, 0, false);
        CollisionFlags collisions = controller.Move(step);

        ground_check_distance = Mathf.Max(v3.Dot((dPos*t + 0.5f*ddPos*t*t), v3.down), 0.1f);
        ground_check_distance = 0.1f;
        is_grounded = check_ground(ground_check_distance, out ground);

        if (is_grounded && (dPos.y < 0))
        {
            dPos.y = ground.distance > 0.01f ? -(ground.distance-0.005f)/t : 0;
            ddPos.y = 0;
        }

        Speed = new v2(dPos.x, dPos.z).magnitude;
        PlayerAnimator.SetFloat("Speed", Speed);

        if (Speed < 0.01)
        {
            dPos.x = 0;
            dPos.z = 0;
        }
    }
    
    bool check_ground(float max_distance, out RaycastHit hit)
    {
        v3 p1 = transform.position + controller.center + v3.down*(controller.height*0.5f - controller.radius);
        v3 p2 = transform.position + controller.center + v3.up  *(controller.height*0.5f - controller.radius);
        bool is_hit = Physics.CapsuleCast(p1, p2, controller.radius, Vector3.down, out hit, max_distance, 1 << LayerMask.NameToLayer("environment"));
        Debug.DrawRay(p1, v3.down * (max_distance+controller.radius), Color.red, 1000);

        return is_hit;
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

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(ColliderCenter, ColliderRadius*2);
    }

    void OnGUI() {
    }
}
