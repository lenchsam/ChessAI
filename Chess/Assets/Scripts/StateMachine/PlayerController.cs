using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    private IState _currentState;
    public bool IsPlayerWhite = true;

    public GameManager Game_Manager;

    public Board BoardScript;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeState(new DefaultState(this));
    }
    private void Update()
    {
        _currentState.UpdateState();
    }
    public void ToggleIsWhite()
    {
        IsPlayerWhite = !IsPlayerWhite;
    }
    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.performed) {
            return;
        }
        _currentState.OnCellClicked();
    }
    public void ChangeState(IState newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }
        _currentState = newState;
        _currentState.Enter();
    }
}
