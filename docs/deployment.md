# Deployment Guide

## Deployment Strategies

### Single Node Deployment

Best for:
- Development environments
- Small-scale applications
- Local testing

```csharp
var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "SingleNodeSystem";
    options.MaxActorCount = 5000;
});

var sp = services.BuildServiceProvider();
var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
var system = await config.InitializeAsync();
```

### Multi-Node Cluster Deployment

Best for:
- High availability requirements
- Load distribution
- Fault tolerance

```csharp
// Node 1 (Primary)
services.AddActorFrameworkCluster(options =>
{
    options.NodeId = "node-1";
    options.BindAddress = "192.168.1.10";
    options.BindPort = 8080;
    options.SeedNodes = new[] { "192.168.1.10:8080" };
});

// Node 2 (Secondary)
services.AddActorFrameworkCluster(options =>
{
    options.NodeId = "node-2";
    options.BindAddress = "192.168.1.11";
    options.BindPort = 8080;
    options.SeedNodes = new[] { "192.168.1.10:8080" };
});
```

## Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0

WORKDIR /app
COPY bin/Release/net10.0/publish .

EXPOSE 8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

CMD ["dotnet", "YourApplication.dll"]
```

### Docker Compose

```yaml
version: '3.8'

services:
  actor-system-1:
    build: .
    container_name: actor-node-1
    environment:
      - NODE_ID=node-1
      - SEED_NODES=actor-system-1:8080
      - BIND_ADDRESS=0.0.0.0
      - BIND_PORT=8080
    ports:
      - "8080:8080"
    volumes:
      - ./data/node1:/data
    depends_on:
      - postgres
      
  actor-system-2:
    build: .
    container_name: actor-node-2
    environment:
      - NODE_ID=node-2
      - SEED_NODES=actor-system-1:8080
      - BIND_ADDRESS=0.0.0.0
      - BIND_PORT=8080
    ports:
      - "8081:8080"
    volumes:
      - ./data/node2:/data
    depends_on:
      - postgres
      
  postgres:
    image: postgres:15-alpine
    container_name: actor-db
    environment:
      POSTGRES_DB: actor_framework
      POSTGRES_USER: actor_user
      POSTGRES_PASSWORD: secure_password
    ports:
      - "5432:5432"
    volumes:
      - ./data/postgres:/var/lib/postgresql/data
```

### Running Docker Compose

```bash
docker-compose up -d

# View logs
docker-compose logs -f actor-system-1

# Stop
docker-compose down
```

## Database Setup

### PostgreSQL

```sql
-- Create database
CREATE DATABASE actor_framework;

-- Create tables
CREATE TABLE actor_snapshots (
    id UUID PRIMARY KEY,
    actor_path VARCHAR(512) NOT NULL,
    state JSONB NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    version INT NOT NULL
);

CREATE TABLE message_log (
    id UUID PRIMARY KEY,
    sender_path VARCHAR(512),
    recipient_path VARCHAR(512) NOT NULL,
    message_type VARCHAR(256) NOT NULL,
    payload JSONB NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL
);

CREATE TABLE actor_metrics (
    id UUID PRIMARY KEY,
    actor_id UUID NOT NULL,
    actor_path VARCHAR(512) NOT NULL,
    messages_processed BIGINT,
    messages_failed BIGINT,
    average_latency DOUBLE PRECISION,
    timestamp TIMESTAMPTZ NOT NULL
);

-- Indexes
CREATE INDEX idx_actor_snapshots_path_timestamp 
    ON actor_snapshots(actor_path, timestamp DESC);

CREATE INDEX idx_message_log_recipient 
    ON message_log(recipient_path, timestamp DESC);

CREATE INDEX idx_actor_metrics_path 
    ON actor_metrics(actor_path, timestamp DESC);
```

### SQL Server

```sql
-- Create database
CREATE DATABASE ActorFramework;
USE ActorFramework;

-- Create tables
CREATE TABLE actor_snapshots (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    actor_path NVARCHAR(512) NOT NULL,
    state NVARCHAR(MAX) NOT NULL,
    timestamp DATETIMEOFFSET NOT NULL,
    version INT NOT NULL
);

CREATE TABLE message_log (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    sender_path NVARCHAR(512),
    recipient_path NVARCHAR(512) NOT NULL,
    message_type NVARCHAR(256) NOT NULL,
    payload NVARCHAR(MAX) NOT NULL,
    timestamp DATETIMEOFFSET NOT NULL
);

-- Indexes
CREATE INDEX idx_actor_snapshots_path_timestamp 
    ON actor_snapshots(actor_path, timestamp DESC);
```

### MySQL

```sql
-- Create database
CREATE DATABASE actor_framework CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE actor_framework;

-- Create tables
CREATE TABLE actor_snapshots (
    id CHAR(36) PRIMARY KEY,
    actor_path VARCHAR(512) NOT NULL,
    state LONGTEXT NOT NULL,
    timestamp DATETIME(6) NOT NULL,
    version INT NOT NULL,
    KEY idx_path_timestamp (actor_path, timestamp DESC)
);

CREATE TABLE message_log (
    id CHAR(36) PRIMARY KEY,
    sender_path VARCHAR(512),
    recipient_path VARCHAR(512) NOT NULL,
    message_type VARCHAR(256) NOT NULL,
    payload LONGTEXT NOT NULL,
    timestamp DATETIME(6) NOT NULL,
    KEY idx_recipient_timestamp (recipient_path, timestamp DESC)
);
```

## Configuration for Production

### High Availability Setup

```csharp
services.AddActorFrameworkReliable(
    "Server=db-primary,db-secondary;" +
    "Database=ActorFramework;" +
    "Pooling=true;" +
    "Max Pool Size=100;" +
    "Connection Timeout=30;" +
    "MultipleActiveResultSets=true"
);

services.Configure<ActorSystemOptions>(options =>
{
    options.SystemName = "ProductionSystem";
    options.MaxActorCount = 50000;
    options.MaxMessageQueueSize = 500000;
    options.EnableMessagePersistence = true;
    options.EnableMetricsCollection = true;
    options.MetricsFlushIntervalMs = 10000;
    options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
    options.BackoffInitialDelayMs = 1000;
    options.BackoffMaxDelayMs = 60000;
});
```

### Performance Tuning

```csharp
// High throughput configuration
services.AddActorFrameworkHighPerformance();

// Or custom tuning
services.Configure<ActorSystemOptions>(options =>
{
    options.MaxMessageQueueSize = 1000000;
    options.MetricsFlushIntervalMs = 30000;
    options.MaxActorCount = 100000;
});
```

## Monitoring and Observability

### Logging Configuration

```csharp
services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Information);
    
    config.AddFilter("DotNetActorFramework", LogLevel.Debug);
    config.AddFilter("DotNetActorFramework.Performance", LogLevel.Information);
});
```

### Health Checks

```csharp
public class ActorSystemHealthCheck : IHealthCheck
{
    private readonly ActorSystem _system;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var health = _system.GetHealthSummary();
        var stats = await _system.GetStatisticsAsync();
        
        if (health.GetHealthPercentage() >= 80)
        {
            return HealthCheckResult.Healthy();
        }
        else if (health.GetHealthPercentage() >= 50)
        {
            return HealthCheckResult.Degraded();
        }
        else
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

services.AddHealthChecks()
    .AddCheck<ActorSystemHealthCheck>("ActorSystem");
```

### Metrics Export

```csharp
// Export metrics periodically
_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            var stats = await actorSystem.GetStatisticsAsync();
            var json = System.Text.Json.JsonSerializer.Serialize(stats);
            
            await System.IO.File.WriteAllTextAsync(
                $"/metrics/{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.json",
                json
            );
            
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metrics export failed: {ex.Message}");
        }
    }
});
```

## Kubernetes Deployment

### Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: actor-system
spec:
  replicas: 3
  selector:
    matchLabels:
      app: actor-system
  template:
    metadata:
      labels:
        app: actor-system
    spec:
      containers:
      - name: actor-system
        image: your-registry/actor-framework:latest
        ports:
        - containerPort: 8080
        env:
        - name: NODE_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: SEED_NODES
          value: "actor-system-0.actor-system.default:8080"
        - name: CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: actor-db
              key: connection-string
        resources:
          requests:
            cpu: 250m
            memory: 512Mi
          limits:
            cpu: 500m
            memory: 1Gi
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: actor-system
spec:
  clusterIP: None
  selector:
    app: actor-system
  ports:
  - port: 8080
    targetPort: 8080
```

### StatefulSet (Preferred for Clustering)

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: actor-system
spec:
  serviceName: actor-system
  replicas: 3
  selector:
    matchLabels:
      app: actor-system
  template:
    metadata:
      labels:
        app: actor-system
    spec:
      containers:
      - name: actor-system
        image: your-registry/actor-framework:latest
        ports:
        - containerPort: 8080
        env:
        - name: NODE_ID
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: SEED_NODES
          value: "actor-system-0.actor-system.default:8080,actor-system-1.actor-system.default:8080"
        volumeMounts:
        - name: data
          mountPath: /data
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 10Gi
```

## Backup and Recovery

### Database Backup

```bash
# PostgreSQL
pg_dump -h localhost -U actor_user -d actor_framework > backup.sql

# SQL Server
sqlcmd -S server -U sa -P password -Q "BACKUP DATABASE ActorFramework TO DISK='/backup/ActorFramework.bak'"

# MySQL
mysqldump -h localhost -u actor_user -p actor_framework > backup.sql
```

### State Recovery

```csharp
public class RecoveryService
{
    public async Task RecoverActorAsync(ActorPath path, 
        IActorStatePersistence persistence)
    {
        var snapshot = await persistence.GetLatestSnapshotAsync(path);
        if (snapshot != null)
        {
            var actor = new MyActor(path);
            await actor.RestoreStateAsync(snapshot);
            // Resume normal operation
        }
    }
}
```

## Scaling Considerations

### Horizontal Scaling

```
Load Balancer
    ↓
┌─────────────────────────────────────┐
│ Node 1: Actors /user/1-1000         │
│ Node 2: Actors /user/1001-2000      │
│ Node 3: Actors /user/2001-3000      │
└─────────────────────────────────────┘
    ↓
  Database
```

### Vertical Scaling

- Increase `MaxActorCount`
- Increase `MaxMessageQueueSize`
- Allocate more CPU/memory
- Optimize middleware pipeline

## Troubleshooting Production Issues

### Out of Memory

1. Reduce `MaxActorCount`
2. Reduce `MaxMessageQueueSize`
3. Implement actor pooling
4. Monitor metrics for memory leaks

### High Latency

1. Check database performance
2. Review supervision strategy
3. Increase thread pool size
4. Profile with ETW/Performance Counters

### Message Loss

1. Enable message persistence
2. Verify database connectivity
3. Check durability settings
4. Implement idempotent operations
