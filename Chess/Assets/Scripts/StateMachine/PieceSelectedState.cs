using UnityEngine;

public class PieceSelectedState : IState
{
    private PlayerController _playerController;
    private Vector2Int _pieceCoords;
    private GameObject _selectedPiece;
    private Camera _mainCamera;

    public PieceSelectedState(PlayerController playerController, Vector2Int pieceCoords)
    {
        _playerController = playerController;
        _pieceCoords = pieceCoords;
        _mainCamera = Camera.main;

        _selectedPiece = _playerController.BoardScript.GetPieceFromPosition(_pieceCoords);
    }

    public void Enter()
    {

    }

    public void Exit()
    {

    }

    public void UpdateState()
    {
        Vector2 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        _selectedPiece.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, -2);

        int x = Mathf.RoundToInt(mouseWorld.x);
        int y = Mathf.RoundToInt(mouseWorld.y);

        //outside of the board
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            return;
        }

        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {

            //drop piece
            Vector2Int roundedCoords = new Vector2Int(Mathf.RoundToInt(mouseWorld.x), Mathf.RoundToInt(mouseWorld.y));
            _playerController.BoardScript.MovePiece(_pieceCoords, roundedCoords, _selectedPiece);
            _playerController.ChangeState(new DefaultState(_playerController));
        }
    }

    public void OnCellClicked()
    {
        
    }
}
