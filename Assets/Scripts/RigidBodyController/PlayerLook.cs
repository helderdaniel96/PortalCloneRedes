﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public Camera camera;
    public float sensitivity = 4f;
    public PlayerController playerController;

    float x = 0;
    float y = 0;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!playerController.Playable) return;
        
        //input
        x += -Input.GetAxis("Mouse Y") * sensitivity;
        y += Input.GetAxis("Mouse X") * sensitivity;

        //clamping
        x = Mathf.Clamp(x, -90, 90);

        //rotation
        camera.transform.localRotation = Quaternion.Euler(x, 0, 0);
        transform.localRotation = Quaternion.Euler(0, y, 0);

        //cursorLocking
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.None;

            if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
        }
    }
}