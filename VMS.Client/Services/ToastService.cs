namespace VMS.Client.Services;

public class ToastService : IToastService
{
    public event Action<ToastMessage>? OnShow;

    public void ShowSuccess(string message, int duration = 5000)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = ToastType.Success,
            Message = message,
            Duration = duration
        });
    }

    public void ShowError(string message, int duration = 5000)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = ToastType.Error,
            Message = message,
            Duration = duration
        });
    }

    public void ShowInfo(string message, int duration = 5000)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = ToastType.Info,
            Message = message,
            Duration = duration
        });
    }

    public void ShowWarning(string message, int duration = 5000)
    {
        OnShow?.Invoke(new ToastMessage
        {
            Type = ToastType.Warning,
            Message = message,
            Duration = duration
        });
    }
}















