using System;

public static class HubEvents
{
    public static event Action OnLoadingStarted;
    public static event Action OnLoadingFinished;

    public static void RaiseLoadingStarted() => OnLoadingStarted?.Invoke();
    public static void RaiseLoadingFinished() => OnLoadingFinished?.Invoke();
}
