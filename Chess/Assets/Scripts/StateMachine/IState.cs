public interface IState
{
    void Enter();
    void Exit();
    void UpdateState();
    void OnCellClicked();
}
