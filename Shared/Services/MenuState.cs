namespace CV.Shared.Services;

public class MenuState
{
    public string? CurrentRoute { get; private set; }

    public event Action? OnChange;

    public void SetRoute(string route)
    {
        CurrentRoute = route;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
        => OnChange?.Invoke();
}
