using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class DefaultState : IState
{
    private PlayerController _playerController;
    public DefaultState(PlayerController playerController)
    {
        _playerController = playerController;
        Debug.Log("set player controller");
    }
    public void Enter()
    {
        Debug.Log("entered default state");
        //throw new System.NotImplementedException();
    }

    public void Exit()
    {
        //throw new System.NotImplementedException();
    }

    public void OnCellClicked()
    {
        Debug.Log("Clicked");
        //throw new System.NotImplementedException();
    }
}
