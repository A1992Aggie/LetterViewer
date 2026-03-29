namespace FamilyPhotos.Services;

public class ErrorService
{
    private readonly List<ErrorMessage> _errors = [];

    public IReadOnlyList<ErrorMessage> Errors => _errors;
    public event Action? OnChange;

    public void Add(string message, bool autoDismiss = true)
    {
        var error = new ErrorMessage
        {
            Message = message,
            Timestamp = DateTime.Now,
            AutoDismiss = autoDismiss
        };
        _errors.Add(error);
        OnChange?.Invoke();

        if (autoDismiss)
        {
            _ = Task.Delay(8000).ContinueWith(_ => Dismiss(error));
        }
    }

    public void Dismiss(ErrorMessage error)
    {
        _errors.Remove(error);
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _errors.Clear();
        OnChange?.Invoke();
    }
}

public class ErrorMessage
{
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool AutoDismiss { get; set; }
}
