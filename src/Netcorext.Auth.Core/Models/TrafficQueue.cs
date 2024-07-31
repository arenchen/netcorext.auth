namespace Netcorext.Auth.Models;

public class TrafficQueue : IDisposable
{
    private readonly Queue<TrafficRaw> _queue = new Queue<TrafficRaw>();

    public event EventHandler? TrafficEnqueued;

    public void Enqueue(TrafficRaw traffic)
    {
        _queue.Enqueue(traffic);

        Task.Run(() => TrafficEnqueued?.Invoke(this, EventArgs.Empty));
    }

    public TrafficRaw Dequeue()
    {
        return _queue.Dequeue();
    }

    public bool TryDequeue(out TrafficRaw? traffic)
    {
        return _queue.TryDequeue(out traffic);
    }

    public void Dispose()
    {
        _queue.Clear();
    }
}
