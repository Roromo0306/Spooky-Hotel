public class MainMenuModel : ModelBase
{
    public enum MenuSelection
    {
        None,
        Play,
        Credits,
        Quit
    }

    private MenuSelection _selected = MenuSelection.None;
    public MenuSelection Selected => _selected;

    private bool _isInteractable = true;
    public bool IsInteractable => _isInteractable;

    public void Select(MenuSelection sel)
    {
        _selected = sel;
        RaiseChange();
    }

    public void SetInteractable(bool state)
    {
        if (_isInteractable == state) return;
        _isInteractable = state;
        RaiseChange();
    }
}

