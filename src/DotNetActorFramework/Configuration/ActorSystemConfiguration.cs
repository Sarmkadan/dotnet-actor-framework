// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Repository;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Configuration and initialization coordinator for the actor system.
/// Manages the lifecycle and coordinates all components.
/// </summary>
public class ActorSystemConfiguration
{
    private readonly ActorSystemOptions _options;
    private readonly ActorRegistry _registry;
    private readonly MailboxService _mailboxService;
    private readonly MessageDispatcher _dispatcher;
    private readonly SupervisionService _supervisionService;
    private readonly ActorStateRepository _stateRepository;
    private readonly MessagePersistenceRepository _messageRepository;
    private readonly ActorMetricsRepository _metricsRepository;
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<ActorSystemConfiguration>? _logger;
    private ActorSystem? _actorSystem;

    public ActorSystemConfiguration(
        ActorSystemOptions options,
        ActorRegistry registry,
        MailboxService mailboxService,
        MessageDispatcher dispatcher,
        SupervisionService supervisionService,
        ActorStateRepository stateRepository,
        MessagePersistenceRepository messageRepository,
        ActorMetricsRepository metricsRepository,
        ConnectionManager connectionManager,
        ILogger<ActorSystemConfiguration>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _mailboxService = mailboxService ?? throw new ArgumentNullException(nameof(mailboxService));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _supervisionService = supervisionService ?? throw new ArgumentNullException(nameof(supervisionService));
        _stateRepository = stateRepository ?? throw new ArgumentNullException(nameof(stateRepository));
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _metricsRepository = metricsRepository ?? throw new ArgumentNullException(nameof(metricsRepository));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger;

        _options.Validate();
    }

    public override string ToString() => $"ActorSystemConfiguration {{ Options = {_options}, Health = {_actorSystem?.GetHealthSummary()}, MailboxStats = {_mailboxService.GetStatistics()}, DispatcherStats = {_dispatcher.GetStatistics()}, SupervisionStats = {_supervisionService.GetStatistics()}, PersistenceStats = {_messageRepository.GetStatistics()} }}";

    /// <summary>
    /// Initializes and creates the actor system.
    /// </summary>
    public async Task<ActorSystem> InitializeAsync()
    {
        try
        {
            _logger?.LogInformation($"Initializing actor system: {_options.SystemName}");

            // Initialize database connection if configured
            if (!string.IsNullOrWhiteSpace(_options.DatabaseConnectionString))
            {
                _connectionManager.Initialize(_options.DatabaseConnectionString);
                var isConnected = await _connectionManager.ValidateConnectionAsync();
                if (!isConnected)
                {
                    _logger?.LogWarning("Database connection validation failed");
                }
            }

            // Create the actor system
            _actorSystem = new ActorSystem(_options.SystemName, _dispatcher);

            _logger?.LogInformation($"Actor system initialized: {_actorSystem.Name} ({_actorSystem.Id:N})");
            _logger?.LogInformation($"Configuration: Mailbox Capacity={_options.DefaultMailboxCapacity}, " +
                $"Persistence={_options.EnableMessagePersistence}, " +
                $"Cluster={_options.EnableClusterMode}");

            return _actorSystem;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize actor system");
            throw new ActorSystemException("Failed to initialize actor system", ex);
        }
    }

    /// <summary>
    /// Creates an actor in the system.
    /// </summary>
    public async Task<ActorRef> CreateActorAsync(ActorPath path, ActorRef? supervisor = null)
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system not initialized. Call InitializeAsync() first.");

        if (path == null)
            throw new ArgumentNullException(nameof(path));

        try
        {
            var actorRef = await _actorSystem.CreateActorAsync(path, supervisor);
            var mailbox = _mailboxService.CreateMailbox(actorRef.Id, _options.DefaultMailboxCapacity);

            _registry.Register(actorRef);

            _logger?.LogInformation($"Actor created: {path} ({actorRef.Id:N})");

            return actorRef;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to create actor: {path}");
            throw;
        }
    }

    /// <summary>
    /// Gets the actor system instance.
    /// </summary>
    public ActorSystem GetActorSystem()
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system not initialized. Call InitializeAsync() first.");

        return _actorSystem;
    }

    /// <summary>
    /// Sends a message between actors.
    /// </summary>
    public async Task SendMessageAsync(ActorRef sender, ActorRef recipient, Message message)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));

        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        try
        {
            await _dispatcher.SendAsync(sender, recipient, message);

            if (_options.EnableMessagePersistence)
            {
                var envelope = new Envelope(message, recipient, sender);
                await _messageRepository.PersistAsync(envelope);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to send message from {sender.Path} to {recipient.Path}");
            throw;
        }
    }

    /// <summary>
    /// Gets system health information.
    /// </summary>
    public SystemHealthSummary GetHealthSummary()
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system not initialized.");

        var summary = _actorSystem.GetHealthSummary();
        _logger?.LogInformation(
            $"System Health: Actors={summary.TotalActors}, " +
            $"Healthy={summary.HealthyActors}, " +
            $"Unhealthy={summary.UnhealthyActors}, " +
            $"Health={summary.GetHealthPercentage():F2}%");

        return summary;
    }

    /// <summary>
    /// Shuts down the actor system.
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (_actorSystem == null)
            return;

        try
        {
            _logger?.LogInformation("Shutting down actor system");

            await _actorSystem.ShutdownAsync();
            _registry.Clear();
            _mailboxService.Clear();
            _messageRepository.Clear();
            _metricsRepository.Clear();

            _logger?.LogInformation("Actor system shutdown complete");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during actor system shutdown");
            throw;
        }
    }

    /// <summary>
    /// Gets comprehensive system statistics.
    /// </summary>
    public SystemStatistics GetStatistics()
    {
        return new SystemStatistics
        {
            Options = _options,
            Health = _actorSystem?.GetHealthSummary(),
            MailboxStats = _mailboxService.GetStatistics(),
            DispatcherStats = _dispatcher.GetStatistics(),
            SupervisionStats = _supervisionService.GetStatistics(),
            PersistenceStats = _messageRepository.GetStatistics(),
            ConnectionStats = _connectionManager.GetStatistics(),
            CollectedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Comprehensive system statistics.
/// </summary>
public class SystemStatistics
{
    public ActorSystemOptions? Options { get; set; }
    public SystemHealthSummary? Health { get; set; }
    public MailboxStatistics? MailboxStats { get; set; }
    public DispatcherStatistics? DispatcherStats { get; set; }
    public SupervisionStatistics? SupervisionStats { get; set; }
    public PersistenceStatistics? PersistenceStats { get; set; }
    public ConnectionStatistics? ConnectionStats { get; set; }
    public DateTime CollectedAt { get; set; }
}
