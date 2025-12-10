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
    }

    public void Exit()
    {
        Debug.Log("exited default state");
    }

    public void OnCellClicked()
    {
        Debug.Log("Clicked");
        //raycast to find clicked cell

        //if the cell has a piece of the players color pick it up
    }
}
