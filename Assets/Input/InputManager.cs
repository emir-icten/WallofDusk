using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    public Vector2 PlayerInput;

    public static Vector2 Movement;

    private InputAction movementAction;

    private void Awake()
    {
        Instance = this;

        var playerInput = GetComponent<PlayerInput>();
        movementAction = playerInput.actions["Move"];
    }

    private void Update()
    {
        Movement = movementAction.ReadValue<Vector2>();
    }
}
