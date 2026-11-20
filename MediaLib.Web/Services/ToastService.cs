namespace MediaLib.Web.Services;

public sealed record ToastMessage(Guid Id, string Message, string Tone);

/// <summary>In-memory notification queue shared by all components in the circuit.</summary>
public sealed class ToastService
{
    public event Action? Changed;

    public IReadOnlyList<ToastMessage> Items { get; private set; } = [];

    public void Success(string message) => Push(message, "success");

    public void Error(string message) => Push(message, "error");

    public void Info(string message) => Push(message, "info");

    public void Warning(string message) => Push(message, "warning");

    public void Dismiss(Guid id)
    {
        Items = Items.Where(t => t.Id != id).ToList();
        Changed?.Invoke();
    }

    private void Push(string message, string tone)
    {
        Items = [.. Items, new ToastMessage(Guid.NewGuid(), message, tone)];
        Changed?.Invoke();
    }
}
