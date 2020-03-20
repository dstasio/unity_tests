// Copyright (c) Davide Stasio

using UnityEngine;
using UnityEngine.InputSystem;

public class player : MonoBehaviour
{
    public Animator PlayerAnimator;
    public camera Camera;
    public float Sensitivity = 500.0f;

    game_controls Controls;

    Vector2 Force;
    Vector2 ddPos;
    Vector2 dPos;
    public float Mass = 70;
    public float FrictionCoefficient = 0.9f;
    public float Speed;

    void Awake()
    {
        Controls = new game_controls();
        Controls.InGame.RotateCamera.performed += ctx => Camera.Input = ctx.ReadValue<Vector2>();
        Controls.InGame.RotateCamera.canceled += _ => Camera.Input = Vector2.zero;

        Controls.InGame.Move.performed += ctx => SetForce(ctx.ReadValue<Vector2>());
        Controls.InGame.Move.canceled += _ => Force = Vector2.zero;

        PlayerAnimator = GetComponentInChildren<Animator>();
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
        Force = new Vector2(Direction.x, Direction.z);
        Force *= Sensitivity;
        //Force = Input*Sensitivity;
    }

    void Update()
    {
        float Weight = Mass * 9.81f;
        Vector2 Friction = -dPos * Weight * FrictionCoefficient;
        Vector2 TotalForce = Force + Friction;
        ddPos = TotalForce / Mass;
    }

    void FixedUpdate()
    {
        float t = Time.fixedDeltaTime;
        Vector2 PosToAdd = dPos*t + 0.5f*ddPos*t*t;
        transform.position += new Vector3(PosToAdd.x, 0.0f, PosToAdd.y);
        dPos += ddPos*t;

        Speed = dPos.magnitude;
        PlayerAnimator.SetFloat("Speed", Speed);

        if (Speed < 0.01)
        {
            dPos = Vector2.zero;
        }
    }

    private void OnEnable() {
        Controls.Enable();
    }

    private void OnDisable() {
        Controls.Disable();
    }
}
