using Microsoft.JSInterop;

namespace IoTNetwork.Pwa.Services;

public sealed class ThemeService(IJSRuntime js)
{
    public bool IsDark { get; private set; }

    public event Action? Changed;

    public async Task SyncFromDomAsync()
    {
        IsDark = await js.InvokeAsync<bool>("iotTheme.isDark");
        Changed?.Invoke();
    }

    public async Task SetDarkAsync(bool dark)
    {
        await js.InvokeVoidAsync("iotTheme.apply", dark);
        IsDark = dark;
        Changed?.Invoke();
    }

    public async Task ToggleAsync()
    {
        IsDark = await js.InvokeAsync<bool>("iotTheme.toggle");
        Changed?.Invoke();
    }
}
