-- =============================================================================
-- Author: Vladyslav Zaiets | https://sarmkadan.com
-- CTO & Software Architect
-- =============================================================================

-- Database initialization script for DotNet Actor Framework
-- Run this script to set up the persistence layer

-- ============================================================================
-- Actor Snapshots Table
-- Stores serialized actor state for recovery
-- ============================================================================
CREATE TABLE IF NOT EXISTS actor_snapshots (
    id UUID PRIMARY KEY,
    actor_path VARCHAR(512) NOT NULL,
    state JSONB NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_actor_latest_snapshot UNIQUE (actor_path, version)
);

CREATE INDEX IF NOT EXISTS idx_actor_snapshots_path_timestamp
    ON actor_snapshots(actor_path, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_actor_snapshots_timestamp
    ON actor_snapshots(timestamp DESC);

-- ============================================================================
-- Message Log Table
-- Stores all messages for durability and replay
-- ============================================================================
CREATE TABLE IF NOT EXISTS message_log (
    id UUID PRIMARY KEY,
    sender_path VARCHAR(512),
    recipient_path VARCHAR(512) NOT NULL,
    message_type VARCHAR(256) NOT NULL,
    payload JSONB NOT NULL,
    correlation_id UUID,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_message_log_recipient_timestamp
    ON message_log(recipient_path, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_message_log_sender_timestamp
    ON message_log(sender_path, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_message_log_correlation_id
    ON message_log(correlation_id);

CREATE INDEX IF NOT EXISTS idx_message_log_timestamp
    ON message_log(timestamp DESC);

-- ============================================================================
-- Actor Metrics Table
-- Stores performance metrics for each actor
-- ============================================================================
CREATE TABLE IF NOT EXISTS actor_metrics (
    id UUID PRIMARY KEY,
    actor_id UUID NOT NULL,
    actor_path VARCHAR(512) NOT NULL,
    messages_processed BIGINT NOT NULL DEFAULT 0,
    messages_failed BIGINT NOT NULL DEFAULT 0,
    average_latency DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    min_latency DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    max_latency DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    p95_latency DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    p99_latency DOUBLE PRECISION NOT NULL DEFAULT 0.0,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT unique_actor_metrics UNIQUE (actor_id, timestamp)
);

CREATE INDEX IF NOT EXISTS idx_actor_metrics_path_timestamp
    ON actor_metrics(actor_path, timestamp DESC);

CREATE INDEX IF NOT EXISTS idx_actor_metrics_actor_id_timestamp
    ON actor_metrics(actor_id, timestamp DESC);

-- ============================================================================
-- Supervision Events Table
-- Tracks supervision strategy applications and actor recovery
-- ============================================================================
CREATE TABLE IF NOT EXISTS supervision_events (
    id UUID PRIMARY KEY,
    actor_id UUID NOT NULL,
    actor_path VARCHAR(512) NOT NULL,
    strategy VARCHAR(64) NOT NULL,
    exception_message TEXT,
    exception_type VARCHAR(256),
    applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    recovery_status VARCHAR(64),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_supervision_events_actor_path
    ON supervision_events(actor_path, applied_at DESC);

CREATE INDEX IF NOT EXISTS idx_supervision_events_applied_at
    ON supervision_events(applied_at DESC);

-- ============================================================================
-- Actor State Table
-- Stores current actor state for quick access
-- ============================================================================
CREATE TABLE IF NOT EXISTS actor_state (
    actor_id UUID PRIMARY KEY,
    actor_path VARCHAR(512) NOT NULL UNIQUE,
    state JSONB,
    actor_state VARCHAR(64) NOT NULL,
    supervisor_path VARCHAR(512),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_actor_state_path
    ON actor_state(actor_path);

CREATE INDEX IF NOT EXISTS idx_actor_state_supervisor
    ON actor_state(supervisor_path);

-- ============================================================================
-- Cleanup Procedure
-- Removes old snapshots and message logs
-- ============================================================================
CREATE OR REPLACE FUNCTION cleanup_old_data()
RETURNS void AS $$
BEGIN
    -- Delete snapshots older than 90 days, keeping at least 1 per actor
    DELETE FROM actor_snapshots
    WHERE timestamp < NOW() - INTERVAL '90 days'
    AND actor_path NOT IN (
        SELECT actor_path
        FROM actor_snapshots
        WHERE timestamp >= NOW() - INTERVAL '90 days'
    );

    -- Delete message logs older than 30 days
    DELETE FROM message_log
    WHERE timestamp < NOW() - INTERVAL '30 days';

    -- Delete supervision events older than 60 days
    DELETE FROM supervision_events
    WHERE created_at < NOW() - INTERVAL '60 days';

    -- Delete metrics older than 7 days
    DELETE FROM actor_metrics
    WHERE created_at < NOW() - INTERVAL '7 days';
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- Scheduled Cleanup Job
-- Runs daily at 2 AM UTC
-- ============================================================================
-- Note: Requires pg_cron extension
-- CREATE EXTENSION IF NOT EXISTS pg_cron;
-- SELECT cron.schedule('cleanup-old-data', '0 2 * * *', 'SELECT cleanup_old_data()');

-- ============================================================================
-- Views for Reporting
-- ============================================================================

-- Actor Health Summary
CREATE OR REPLACE VIEW v_actor_health_summary AS
SELECT
    COUNT(DISTINCT actor_id) as total_actors,
    SUM(CASE WHEN actor_state = 'Started' THEN 1 ELSE 0 END) as running_actors,
    SUM(CASE WHEN actor_state = 'Terminated' THEN 1 ELSE 0 END) as terminated_actors,
    SUM(CASE WHEN actor_state = 'Error' THEN 1 ELSE 0 END) as error_actors,
    ROUND(
        100.0 * SUM(CASE WHEN actor_state = 'Started' THEN 1 ELSE 0 END) /
        NULLIF(COUNT(DISTINCT actor_id), 0),
        2
    ) as health_percentage
FROM actor_state;

-- Message Processing Stats
CREATE OR REPLACE VIEW v_message_stats AS
SELECT
    COUNT(DISTINCT id) as total_messages,
    SUM(CASE WHEN processed_at IS NOT NULL THEN 1 ELSE 0 END) as processed_messages,
    SUM(CASE WHEN error_message IS NOT NULL THEN 1 ELSE 0 END) as failed_messages,
    ROUND(
        100.0 * SUM(CASE WHEN processed_at IS NOT NULL THEN 1 ELSE 0 END) /
        NULLIF(COUNT(DISTINCT id), 0),
        2
    ) as success_rate,
    DATE_TRUNC('hour', timestamp) as hour
FROM message_log
GROUP BY DATE_TRUNC('hour', timestamp);

-- Supervision Stats
CREATE OR REPLACE VIEW v_supervision_stats AS
SELECT
    strategy,
    COUNT(*) as count,
    SUM(CASE WHEN recovery_status = 'Success' THEN 1 ELSE 0 END) as successful_recoveries,
    SUM(CASE WHEN recovery_status = 'Failed' THEN 1 ELSE 0 END) as failed_recoveries
FROM supervision_events
WHERE applied_at > NOW() - INTERVAL '24 hours'
GROUP BY strategy;

-- ============================================================================
-- Grant Permissions
-- =============================================================================
-- Uncomment to grant permissions to application user
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO actor_user;
-- GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO actor_user;
-- GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO actor_user;
