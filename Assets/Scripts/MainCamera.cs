using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    [SerializeField] private float cameraHeight = 10;
    [SerializeField] private float targetOrthSize = 5;
    [SerializeField] private float maxOrthSize = 50;
    private Camera cam;

    //orthographic size
    private float minOrthSize = 5;

    //camera rotation
    private float tiltAmount = 270f;

    //camera movement
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float moveSpeedLerp = 5f;
    private Vector3 moveTargetPosition; 

    void Start()
    {
        cam = Camera.main;
        cam.transform.position = new Vector3(0, cameraHeight, 0);
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.orthographic = true;
        cam.orthographicSize = targetOrthSize;

        moveTargetPosition = transform.position;
    }

    void Update()
    {
        ModifyOrthSize();
        RotateCamera();
        MoveCamera();
    }

    //Funkcja modyfikuj¹ca zakres widzenia kamery w zale¿noœci od wartoœci min/max i inputu scrollem
    private void ModifyOrthSize()
    {
        float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
        if (scrollAxis < 0)
            targetOrthSize += 2f;
        else if (scrollAxis > 0)
            targetOrthSize -= 2f;

        targetOrthSize = Math.Clamp(targetOrthSize, minOrthSize, maxOrthSize);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthSize, 5f * Time.deltaTime);
    }

    private void RotateCamera()
    {
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            if (mouseX == 0)
                return;

            Vector3 euler = cam.transform.eulerAngles;
            euler.y += mouseX * tiltAmount * Time.deltaTime;
            cam.transform.eulerAngles = euler;
        }
    }

    private void MoveCamera()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical);
        Quaternion rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        direction = rotation * direction;
        direction.Normalize();

        moveTargetPosition += moveSpeed * Time.deltaTime * direction * targetOrthSize/maxOrthSize;

        cam.transform.position = Vector3.Lerp(cam.transform.position, moveTargetPosition, moveSpeedLerp * Time.deltaTime);
    }

}
