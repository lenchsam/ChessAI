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
        Vector3 mouseScreenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreenPos);

        _selectedPiece.transform.position = new Vector3(mouseWorld.x, mouseWorld.y, -2);

        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
        {
            int x = Mathf.RoundToInt(mouseWorld.x);
            int y = Mathf.RoundToInt(mouseWorld.y);

            if (x < 0 || x >= 8 || y < 0 || y >= 8)
            {
                _selectedPiece.transform.position = new Vector3(_pieceCoords.x, _pieceCoords.y, -1);
                _playerController.ChangeState(new DefaultState(_playerController));
                return;
            }

            Vector2Int newCoords = new Vector2Int(x, y);

            _playerController.BoardScript.MovePieceVisual(_pieceCoords, newCoords);
            _playerController.BoardScript.Bitboards.MovePiece(_pieceCoords, newCoords);

            _playerController.ChangeState(new DefaultState(_playerController));
        }
    }

    public void OnCellClicked()
    {
        
    }
}
