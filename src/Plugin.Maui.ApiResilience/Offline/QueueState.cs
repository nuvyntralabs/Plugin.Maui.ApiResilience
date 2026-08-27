namespace Plugin.Maui.ApiResilience;

internal sealed class QueueState
{
    public List<QueuedRequest> Pending { get; set; } = [];

    public List<QueuedRequest> DeadLetter { get; set; } = [];
}
