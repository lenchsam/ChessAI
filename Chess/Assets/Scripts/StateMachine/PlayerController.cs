using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerController : MonoBehaviour
{

    private IState _currentState;

    public UnityEvent OnCellClicked = new UnityEvent();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeState(new DefaultState(this));
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
