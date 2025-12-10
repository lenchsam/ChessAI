using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    private IState _currentState;
    public bool IsPlayerWhite = true;

    public Board BoardScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (BoardScript == null)
            BoardScript = FindFirstObjectByType<Board>();

        ChangeState(new DefaultState(this));
    }
    private void Update()
    {
        _currentState.UpdateState();
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
