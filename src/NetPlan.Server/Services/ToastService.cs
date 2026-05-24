namespace NetPlan.Server.Services;

public enum ToastType { Success, Error, Warning, Info }

public class ToastMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = "";
    public ToastType Type { get; set; } = ToastType.Info;
    public int DurationMs { get; set; } = 3000;
}

public class ToastService
{
    public event Action<ToastMessage>? OnShow;

    public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000)
    {
        OnShow?.Invoke(new ToastMessage { Message = message, Type = type, DurationMs = durationMs });
    }

    public void Success(string message) => Show(message, ToastType.Success, 3000);
    public void Error(string message) => Show(message, ToastType.Error, 5000);
    public void Warning(string message) => Show(message, ToastType.Warning, 4000);
    public void Info(string message) => Show(message, ToastType.Info, 3000);
}
