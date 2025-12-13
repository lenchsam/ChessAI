using UnityEngine;

public class DefaultState : IState
{
    private PlayerController _playerController;
    private Camera _mainCamera;

    public DefaultState(PlayerController playerController)
    {
        _playerController = playerController;
        _mainCamera = Camera.main;
    }
    public void Enter()
    {
        
    }

    public void Exit()
    {
        
    }

    public void UpdateState()
    {

    }

    public void OnCellClicked()
    {
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        Vector2 mouseWorldPos = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        int x = Mathf.RoundToInt(mouseWorldPos.x);
        int y = Mathf.RoundToInt(mouseWorldPos.y);

        if(x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return;
        }

        //char pieceChar = _playerController.BitBoards.GetPieceAt(x, y);

        //if (pieceChar != '\0')
        //{

        //    //if belongs to the player
        //    bool isWhitePiece = char.IsUpper(pieceChar);

        //    if (isWhitePiece == _playerController.IsPlayerWhite)
        //    {
        //        _playerController.ChangeState(new PieceSelectedState(_playerController, new Vector2Int(x, y)));
        //    }
        //}
    }
}
