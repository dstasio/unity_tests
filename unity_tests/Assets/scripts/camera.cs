﻿// Copyright (c) Davide Stasio

using UnityEngine;
using UnityEngine.InputSystem;

public class camera : MonoBehaviour
{
    public Transform PlayerBody;
    public float PlayerHeadHeight = 1.8f;
    public float CameraDistance = 3f, CameraOffsetFactor = 0.33f;
    private float Pitch = 0f, Yaw = 0f;

    public Vector2 Input;
    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;

        transform.localPosition = new Vector3(0.0f, 1.0f, -CameraDistance);
        transform.localPosition += transform.right * CameraOffsetFactor;
        transform.LookAt(PlayerBody.position + new Vector3(0, PlayerHeadHeight, 0));
    }

    void Update()
    {
        float CameraSensitivity = Time.deltaTime * 2.0f * Mathf.PI * 0.3f;
        Yaw += Input.x * CameraSensitivity;
        Pitch += Input.y * CameraSensitivity;
        Pitch = Mathf.Clamp(Pitch, -Mathf.PI / 2.1f, Mathf.PI / 2.1f);

        float CameraX, CameraY, CameraZ;
        CameraY = Mathf.Sin(Pitch);
        CameraX = Mathf.Cos(Yaw) * Mathf.Cos(Pitch);
        CameraZ = Mathf.Sin(Yaw) * Mathf.Cos(Pitch);

        Vector3 CameraPosition = new Vector3(CameraX, CameraY, CameraZ);
        CameraPosition.Normalize();
        CameraPosition *= CameraDistance;
        if (CameraPosition.y <= 0.1f)
        {
            CameraPosition.y = 0.1f;
        }

        transform.localPosition = CameraPosition;
        transform.LookAt(PlayerBody);
        transform.LookAt(PlayerBody.position + new Vector3(0, PlayerHeadHeight, 0));
        transform.localPosition += transform.right * CameraOffsetFactor;
    }
}
