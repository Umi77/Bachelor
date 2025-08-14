using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Legacy;
using UnityEngine;

public class OceanBody : MonoBehaviour
{
    public Transform[] bouyencies = new Transform[4];
    public CharacterController characterController;
    public Transform body;
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;
    
    void Update()
    {
        SetHeight();
        SetRotation();

        if (characterController != null) 
        {
            float turnInput = Input.GetAxis("Horizontal");
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                Vector3 forwardMove = transform.forward * moveSpeed * Time.deltaTime;
                characterController.Move(forwardMove);
            }
        }
    }
    
    private void SetHeight()
    {
        Vector3 nHeight = body.position;
        float height = 0f;
        for (int i = 0; i < bouyencies.Length; i++)
        {
            height += bouyencies[i].position.y;
        }
        nHeight.y = height / bouyencies.Length;
        body.position = nHeight;

    }

    private void SetRotation()
    {
        // Normal per triangle
        Vector3 sum = new Vector3(0, 0, 0);
        for (int i = 0; i < 2; i++)
        {
            sum += FaceNormalWeighted(bouyencies[0 + i + i].position, bouyencies[1].position, bouyencies[2 + i].position);
        }
        sum = sum.normalized;
        if (sum.sqrMagnitude > 0 && body.up.sqrMagnitude > 0f)
        {
            Quaternion rotation = Quaternion.FromToRotation(body.up, sum);

            // convert delta to axis-angle
            rotation.ToAngleAxis(out float angle, out Vector3 axis);

            // map angle to [-180, 180] then clamp to [-30, 30]
            if (angle > 180f) angle -= 360f;
            float clampedAngle = Mathf.Clamp(angle, -30f, 30f);

            // rebuild limited delta and apply
            Quaternion limitedDelta = Quaternion.AngleAxis(clampedAngle, axis);
            body.rotation = limitedDelta * body.rotation;
        }
    }

    private Vector3 FaceNormalWeighted(Vector3 a, Vector3 b, Vector3 c)
    {
        // Unnormalisiert: Richtung * (2 * Dreiecksfläche)
        return Vector3.Cross(b - a, c - a);
    }
}
