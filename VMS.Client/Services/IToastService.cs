namespace VMS.Client.Services;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastMessage
{
    public ToastType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public int Duration { get; set; } = 5000; // milliseconds
}

public interface IToastService
{
    event Action<ToastMessage>? OnShow;
    void ShowSuccess(string message, int duration = 5000);
    void ShowError(string message, int duration = 5000);
    void ShowInfo(string message, int duration = 5000);
    void ShowWarning(string message, int duration = 5000);
}


















