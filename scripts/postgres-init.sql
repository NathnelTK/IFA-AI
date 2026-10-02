-- PostgreSQL initialization script for IFA-AI application
-- This script runs when the PostgreSQL container starts for the first time

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Set timezone to UTC
SET timezone = 'UTC';

-- Create additional indexes for performance
-- These will be created by Entity Framework migrations as well,
-- but having them here ensures they exist early in the setup process

-- Performance optimizations
ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements';
ALTER SYSTEM SET max_connections = 100;
ALTER SYSTEM SET shared_buffers = '256MB';
ALTER SYSTEM SET effective_cache_size = '1GB';
ALTER SYSTEM SET maintenance_work_mem = '64MB';
ALTER SYSTEM SET random_page_cost = 1.1;
ALTER SYSTEM SET effective_io_concurrency = 200;

-- Create database if it doesn't exist (handled by POSTGRES_DB env var)
-- This is mainly for documentation purposes
SELECT 'Database initialization completed' as status;