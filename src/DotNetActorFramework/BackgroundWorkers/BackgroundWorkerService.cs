// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetActorFramework.BackgroundWorkers;

/// <summary>
/// Interface for background work tasks.
/// Background workers execute asynchronously to handle non-blocking work.
/// </summary>
public interface IBackgroundWorker
{
    /// <summary>
    /// Unique identifier for the worker.
    /// </summary>
    string WorkerId { get; }

    /// <summary>
    /// Executes the background work.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the interval at which this worker should execute.
    /// </summary>
    TimeSpan Interval { get; }

    /// <summary>
    /// Called when the worker is starting.
    /// </summary>
    Task OnStartAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when the worker is stopping.
    /// </summary>
    Task OnStopAsync() => Task.CompletedTask;
}

/// <summary>
/// Service that manages and executes background workers.
/// Handles scheduling, error handling, and lifecycle management.
/// </summary>
public class BackgroundWorkerService : IDisposable
{
    private readonly ConcurrentDictionary<string, WorkerTask> _workers = [];
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationSources = [];
    private bool _isRunning = false;

    /// <summary>
    /// Registers a background worker.
    /// </summary>
    public void RegisterWorker(IBackgroundWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        if (string.IsNullOrWhiteSpace(worker.WorkerId))
            throw new ArgumentException("Worker ID cannot be empty.", nameof(worker));

        var workerTask = new WorkerTask(worker);
        _workers.TryAdd(worker.WorkerId, workerTask);
    }

    /// <summary>
    /// Unregisters a worker by ID.
    /// </summary>
    public bool UnregisterWorker(string workerId)
    {
        if (string.IsNullOrWhiteSpace(workerId))
            return false;

        return _workers.TryRemove(workerId, out _);
    }

    /// <summary>
    /// Starts all registered workers.
    /// </summary>
    public async Task StartAsync()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        foreach (var kvp in _workers)
        {
            var worker = kvp.Value.Worker;
            var cts = new CancellationTokenSource();
            _cancellationSources[kvp.Key] = cts;

            try
            {
                await worker.OnStartAsync().ConfigureAwait(false);
                ExecuteWorkerLoop(kvp.Key, worker, cts.Token);
            }
            catch (Exception ex)
            {
                // Log error but continue starting other workers
                System.Diagnostics.Debug.WriteLine($"Error starting worker {kvp.Key}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Stops all workers.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        foreach (var kvp in _cancellationSources)
        {
            kvp.Value.Cancel();
        }

        // Wait for workers to complete (with timeout)
        var completionTasks = _workers.Values
            .Where(wt => wt.Task != null)
            .Select(wt => Task.WhenAny(wt.Task, Task.Delay(5000)))
            .ToList();

        if (completionTasks.Count > 0)
            await Task.WhenAll(completionTasks).ConfigureAwait(false);

        // Call stop handlers
        foreach (var kvp in _workers)
        {
            try
            {
                await kvp.Value.Worker.OnStopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping worker {kvp.Key}: {ex.Message}");
            }
        }

        _cancellationSources.Clear();
    }

    /// <summary>
    /// Gets the status of a worker.
    /// </summary>
    public WorkerStatus? GetWorkerStatus(string workerId)
    {
        if (!_workers.TryGetValue(workerId, out var workerTask))
            return null;

        return new WorkerStatus
        {
            WorkerId = workerId,
            IsRunning = _isRunning && !_cancellationSources[workerId].Token.IsCancellationRequested,
            LastExecutedAt = workerTask.LastExecutedAt,
            ExecutionCount = workerTask.ExecutionCount,
            ErrorCount = workerTask.ErrorCount,
            LastError = workerTask.LastError
        };
    }

    private async void ExecuteWorkerLoop(string workerId, IBackgroundWorker worker, CancellationToken ct)
    {
        if (!_workers.TryGetValue(workerId, out var workerTask))
            return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await worker.ExecuteAsync(ct).ConfigureAwait(false);
                workerTask.LastExecutedAt = DateTime.UtcNow;
                workerTask.ExecutionCount++;
                workerTask.LastError = null;

                await Task.Delay(worker.Interval, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                workerTask.ErrorCount++;
                workerTask.LastError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Worker {workerId} error: {ex.Message}");

                // Wait before retry
                try { await Task.Delay(worker.Interval, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        workerTask.Task = null;
    }

    public void Dispose()
    {
        StopAsync().Wait(TimeSpan.FromSeconds(10));
        foreach (var cts in _cancellationSources.Values)
            cts?.Dispose();
    }

    private class WorkerTask
    {
        public IBackgroundWorker Worker { get; }
        public Task? Task { get; set; }
        public DateTime? LastExecutedAt { get; set; }
        public long ExecutionCount { get; set; }
        public long ErrorCount { get; set; }
        public string? LastError { get; set; }

        public WorkerTask(IBackgroundWorker worker)
        {
            Worker = worker;
        }
    }
}

/// <summary>
/// Status information for a background worker.
/// </summary>
public class WorkerStatus
{
    public string WorkerId { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public long ExecutionCount { get; set; }
    public long ErrorCount { get; set; }
    public string? LastError { get; set; }
}
