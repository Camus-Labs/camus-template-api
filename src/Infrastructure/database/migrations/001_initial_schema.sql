-- =============================================================================
-- Camus PostgreSQL Database Schema - Initial Migration
-- =============================================================================
-- Migration: 001
-- Description: Initial database schema with API info and authorization tables
-- Date: 2026-02-16
-- 
-- Usage:
--   psql -U postgres -d camus -f 001_initial_schema.sql
--
-- Prerequisites:
--   - PostgreSQL 12 or higher
--   - Database 'camus' must exist (CREATE DATABASE camus;)
-- =============================================================================

-- =============================================================================
-- CREATE SCHEMAS
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS camus;

-- =============================================================================
-- API INFO TABLES
-- =============================================================================

-- Table: camus.api_info
-- Stores API version information including features and status
CREATE TABLE IF NOT EXISTS camus.api_info (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL DEFAULT 'My Basic API',
    version VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(100) NOT NULL,
    features TEXT[], -- Array of feature strings
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for version lookup (most common query)
CREATE INDEX IF NOT EXISTS idx_api_info_version ON camus.api_info(version);

-- =============================================================================
-- AUTHORIZATION TABLES
-- =============================================================================

-- Table: camus.roles
-- Stores role definitions
CREATE TABLE IF NOT EXISTS camus.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for role name lookup
CREATE INDEX IF NOT EXISTS idx_roles_name ON camus.roles(name);

-- Table: camus.role_permissions
-- Stores permissions associated with roles (many-to-many)
CREATE TABLE IF NOT EXISTS camus.role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL,
    permission VARCHAR(200) NOT NULL,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) 
        REFERENCES camus.roles(id) ON DELETE CASCADE,
    CONSTRAINT uq_role_permission UNIQUE (role_id, permission)
);

-- Index for role_id lookup
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON camus.role_permissions(role_id);

-- Table: camus.users
-- Stores user information with bcrypt password hashes
CREATE TABLE IF NOT EXISTS camus.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(200) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    last_login TIMESTAMP,  -- Timestamp of last successful login
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for username lookup (most common query)
CREATE INDEX IF NOT EXISTS idx_users_username ON camus.users(username);

-- Table: camus.user_roles
-- Associates users with roles (many-to-many)
CREATE TABLE IF NOT EXISTS camus.user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) 
        REFERENCES camus.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) 
        REFERENCES camus.roles(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_role UNIQUE (user_id, role_id)
);

-- Indexes for user_roles lookups
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON camus.user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON camus.user_roles(role_id);

-- =============================================================================
-- AUDIT TABLES
-- =============================================================================

-- Table: camus.action_audit
-- Stores explicit business actions for compliance and debugging
CREATE TABLE IF NOT EXISTS camus.action_audit (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID,
    user_name VARCHAR(200),
    trace_id VARCHAR(32),  -- OpenTelemetry trace ID for correlation
    action_title VARCHAR(200) NOT NULL,
    action_summary TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes for audit queries
CREATE INDEX IF NOT EXISTS idx_action_audit_user_id ON camus.action_audit(user_id);
CREATE INDEX IF NOT EXISTS idx_action_audit_trace_id ON camus.action_audit(trace_id) WHERE trace_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_action_audit_created_at ON camus.action_audit(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_action_audit_action_title ON camus.action_audit(action_title);

-- =============================================================================
-- AUTOMATIC AUDIT TRIGGERS
-- =============================================================================

-- Function to automatically update timestamp and user tracking fields
CREATE OR REPLACE FUNCTION update_audit_fields()
RETURNS TRIGGER AS $$
DECLARE
    v_username VARCHAR(200);
BEGIN
    -- Get username from session variable (set by application from JWT token)
    BEGIN
        v_username := current_setting('app.current_username', true);
    EXCEPTION
        WHEN OTHERS THEN
            v_username := NULL;
    END;
    
    -- Handle INSERT
    IF (TG_OP = 'INSERT') THEN
        NEW.created_at := NOW();
        NEW.updated_at := NOW();
        NEW.created_by := v_username;
        NEW.updated_by := v_username;
        RETURN NEW;
    -- Handle UPDATE
    ELSIF (TG_OP = 'UPDATE') THEN
        NEW.created_at := OLD.created_at;  -- Preserve original created_at
        NEW.created_by := OLD.created_by;  -- Preserve original created_by
        NEW.updated_at := NOW();
        NEW.updated_by := v_username;
        RETURN NEW;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply audit trigger to all tables with audit fields
-- All tables now use the same function since they all have the full set of audit columns
CREATE TRIGGER users_audit_trigger
BEFORE INSERT OR UPDATE ON camus.users
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER roles_audit_trigger
BEFORE INSERT OR UPDATE ON camus.roles
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER api_info_audit_trigger
BEFORE INSERT OR UPDATE ON camus.api_info
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER role_permissions_audit_trigger
BEFORE INSERT OR UPDATE ON camus.role_permissions
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER user_roles_audit_trigger
BEFORE INSERT OR UPDATE ON camus.user_roles
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

-- =============================================================================
-- SAMPLE DATA (Optional - for development/testing)
-- =============================================================================

-- Begin transaction for data insertion
-- If any insert fails, all data changes will be rolled back
BEGIN;

-- Set session variable so audit triggers populate created_by/updated_by as 'Admin'
SELECT set_config('app.current_username', 'Admin', true);

-- Sample API info data
-- Note: API info must match configuration in appsettings.json > AppDataSettings.InMemory.ApiInfos
INSERT INTO camus.api_info (version, name, status, features) VALUES
    ('1.0', 'Camus DB API - 1.0 Release', 'Available', 
     ARRAY['Basic API Information', 'Public Endpoints', 'Basic Observability']),
    ('2.0', 'Camus DB API - 2.0 Beta', 'Beta', 
     ARRAY['Authentication (JWT & API Key)', 'Authorization (Role-based)', 'API Versioning', 
           'Observability (OpenTelemetry)', 'Rate Limiting (Multiple policies)', 'Swagger/OpenAPI Documentation',
           'Secret Management (Dapr)', 'Error Handling', 'CORS Support', 'Health Checks']);

-- Sample roles
-- Note: Role definitions must match configuration in appsettings.json > Authorization.InMemory.Roles
INSERT INTO camus.roles (id, name, description) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'Administrator with full access including token creation'),
    ('22222222-2222-2222-2222-222222222222', 'ReadWrite', 'Standard user with read and write access'),
    ('33333333-3333-3333-3333-333333333333', 'ReadOnly', 'User with read-only access');

-- Sample permissions for Admin role
-- Note: Permission values must match constants defined in emc.camus.application.Auth.Permissions class
--       Available permissions: api.read, api.write, token.create
INSERT INTO camus.role_permissions (role_id, permission) VALUES
    ('11111111-1111-1111-1111-111111111111', 'token.create'),
    ('11111111-1111-1111-1111-111111111111', 'api.read'),
    ('11111111-1111-1111-1111-111111111111', 'api.write');

-- Sample permissions for ReadWrite role
INSERT INTO camus.role_permissions (role_id, permission) VALUES
    ('22222222-2222-2222-2222-222222222222', 'api.read'),
    ('22222222-2222-2222-2222-222222222222', 'api.write');

-- Sample permissions for ReadOnly role
INSERT INTO camus.role_permissions (role_id, permission) VALUES
    ('33333333-3333-3333-3333-333333333333', 'api.read');

-- Sample users with bcrypt password hashes
-- Note: These use predefined UUIDs for development. In production, use gen_random_uuid()
-- Default passwords (for development only):
--   Admin: adminsecret
--   ClientApp: clientsecret
-- 
-- Hashes generated with bcrypt work factor 12
INSERT INTO camus.users (id, username, password_hash) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Admin', '$2a$12$o/lEizsiyXbUjG5dSijR2OHUo7f6zjci179AXOyeYT5V.ii2C48gi'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ClientApp', '$2a$12$bEdsi67xom.wkxmPk6QvyO3/G0XtLqEHjMBNqn79UtakMWZOIQvFi');

-- Assign roles to users
INSERT INTO camus.user_roles (user_id, role_id) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111'), -- Admin has Admin role
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222');  -- ClientApp has ReadWrite role

-- Commit transaction - all sample data inserted successfully
COMMIT;

-- =============================================================================
-- COMMENTS ON TABLES
-- =============================================================================

COMMENT ON TABLE camus.api_info IS 'Stores API version information, features, and status';
COMMENT ON TABLE camus.roles IS 'Stores role definitions for authorization';
COMMENT ON TABLE camus.role_permissions IS 'Stores permissions associated with each role';
COMMENT ON TABLE camus.users IS 'Stores user accounts with bcrypt password hashes';
COMMENT ON TABLE camus.user_roles IS 'Associates users with their assigned roles';
COMMENT ON TABLE camus.action_audit IS 'Stores explicit business actions for audit trail and debugging';

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
-- Uncomment to verify the schema was created successfully:

-- SELECT 'API Info Count:' as check_name, COUNT(*) as result FROM camus.api_info;
-- SELECT 'Roles Count:' as check_name, COUNT(*) as result FROM camus.roles;
-- SELECT 'Users Count:' as check_name, COUNT(*) as result FROM camus.users;
-- SELECT 'User Roles Count:' as check_name, COUNT(*) as result FROM camus.user_roles;
-- SELECT 'Role Permissions Count:' as check_name, COUNT(*) as result FROM camus.role_permissions;
-- SELECT 'Action Audit Count:' as check_name, COUNT(*) as result FROM camus.action_audit;

-- =============================================================================
-- MIGRATION COMPLETE
-- =============================================================================
