using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    public float normalSpeed = 5.0f;
    public float sprintSpeed = 10.0f;
    public float lookSpeed = 2.0f;

    float headX = 0.0f, headY = 0.0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Vector3 forward = transform.TransformDirection(Vector3.forward),
        //     right = transform.TransformDirection(Vector3.right);
        // forward.y = 0;
        // forward = Vector3.Normalize(forward);
        // right.y = 0;
        // right = Vector3.Normalize(right);

        // var isRunning = Input.GetKey(KeyCode.LeftControl);
        // var deltaX = (isRunning ? sprintSpeed : normalSpeed) * Input.GetAxis("Horizontal");
        // var deltaY = (isRunning ? sprintSpeed : normalSpeed) * Input.GetAxis("Vertical");
        
        // var deltaZ = 0.0f;
        // if (Input.GetKey(KeyCode.LeftShift)) deltaZ -= normalSpeed;
        // if (Input.GetKey(KeyCode.Space)) deltaZ += normalSpeed;

        // Vector3 deltaMove = deltaX * right + deltaY * forward + deltaZ * Vector3.up;

        // transform.localPosition += deltaMove * Time.deltaTime;

        // headY += -Input.GetAxis("Mouse Y") * lookSpeed;
        // headY = Mathf.Clamp(headY, -90, 90);
        // headX += Input.GetAxis("Mouse X") * lookSpeed;

        // transform.localRotation = Quaternion.Euler(headY, headX, 0);
    }
}
