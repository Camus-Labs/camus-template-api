-- =============================================================================
-- Camus PostgreSQL Database Schema
-- =============================================================================
-- This script creates the database schema for Camus application data and 
-- authorization persistence.
--
-- Usage:
--   psql -U postgres -d camus -f schema.sql
--
-- Prerequisites:
--   - PostgreSQL 12 or higher
--   - Database 'camus' must exist (CREATE DATABASE camus;)
-- =============================================================================

-- =============================================================================
-- API INFO TABLES
-- =============================================================================

-- Table: api_info
-- Stores API version information including features and status
CREATE TABLE IF NOT EXISTS api_info (
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
CREATE INDEX IF NOT EXISTS idx_api_info_version ON api_info(version);

-- =============================================================================
-- AUTHORIZATION TABLES
-- =============================================================================

-- Table: roles
-- Stores role definitions
CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for role name lookup
CREATE INDEX IF NOT EXISTS idx_roles_name ON roles(name);

-- Table: role_permissions
-- Stores permissions associated with roles (many-to-many)
CREATE TABLE IF NOT EXISTS role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL,
    permission VARCHAR(200) NOT NULL,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
    CONSTRAINT fk_role_permissions_role FOREIGN KEY (role_id) 
        REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT uq_role_permission UNIQUE (role_id, permission)
);

-- Index for role_id lookup
CREATE INDEX IF NOT EXISTS idx_role_permissions_role_id ON role_permissions(role_id);

-- Table: users
-- Stores user information with bcrypt password hashes
CREATE TABLE IF NOT EXISTS users (
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
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Table: user_roles
-- Associates users with roles (many-to-many)
CREATE TABLE IF NOT EXISTS user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    created_by VARCHAR(200),  -- Username who created the record
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(200) ,  -- Username who last updated the record
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
    CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) 
        REFERENCES roles(id) ON DELETE CASCADE,
    CONSTRAINT uq_user_role UNIQUE (user_id, role_id)
);

-- Indexes for user_roles lookups
CREATE INDEX IF NOT EXISTS idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_user_roles_role_id ON user_roles(role_id);

-- =============================================================================
-- ACTION AUDIT TABLE
-- =============================================================================

-- Table: action_audit
-- Stores explicit business actions for compliance and debugging
CREATE TABLE IF NOT EXISTS action_audit (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    user_name VARCHAR(200),
    trace_id VARCHAR(32),  -- OpenTelemetry trace ID for correlation
    action_title VARCHAR(200) NOT NULL,
    action_summary TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Indexes for audit queries
CREATE INDEX IF NOT EXISTS idx_action_audit_user_id ON action_audit(user_id);
CREATE INDEX IF NOT EXISTS idx_action_audit_trace_id ON action_audit(trace_id) WHERE trace_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_action_audit_created_at ON action_audit(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_action_audit_action_title ON action_audit(action_title);

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
BEFORE INSERT OR UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER roles_audit_trigger
BEFORE INSERT OR UPDATE ON roles
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER api_info_audit_trigger
BEFORE INSERT OR UPDATE ON api_info
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER role_permissions_audit_trigger
BEFORE INSERT OR UPDATE ON role_permissions
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

CREATE TRIGGER user_roles_audit_trigger
BEFORE INSERT OR UPDATE ON user_roles
FOR EACH ROW EXECUTE FUNCTION update_audit_fields();

-- =============================================================================
-- SAMPLE DATA (Optional - for development/testing)
-- =============================================================================

-- Sample API info data
-- Note: API info must match configuration in appsettings.json > AppDataSettings.InMemory.ApiInfos
INSERT INTO api_info (version, name, status, features, created_by, updated_by) VALUES
    ('1.0', 'Camus DB API - 1.0 Release', 'Available', 
     ARRAY['Basic API Information', 'Public Endpoints', 'Basic Observability'], 'Admin', 'Admin'),
    ('2.0', 'Camus DB API - 2.0 Beta', 'Beta', 
     ARRAY['Authentication (JWT & API Key)', 'Authorization (Role-based)', 'API Versioning', 
           'Observability (OpenTelemetry)', 'Rate Limiting (Multiple policies)', 'Swagger/OpenAPI Documentation',
           'Secret Management (Dapr)', 'Error Handling', 'CORS Support', 'Health Checks'], 'Admin', 'Admin')
ON CONFLICT (version) DO NOTHING;

-- Sample roles
-- Note: Role definitions must match configuration in appsettings.json > Authorization.InMemory.Roles
INSERT INTO roles (id, name, description, created_by, updated_by) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'Administrator with full access including token creation', 'Admin', 'Admin'),
    ('22222222-2222-2222-2222-222222222222', 'ReadWrite', 'Standard user with read and write access', 'Admin', 'Admin'),
    ('33333333-3333-3333-3333-333333333333', 'ReadOnly', 'User with read-only access', 'Admin', 'Admin')
ON CONFLICT (name) DO NOTHING;

-- Sample permissions for Admin role
-- Note: Permission values must match constants defined in emc.camus.application.Auth.Permissions class
--       Available permissions: api.read, api.write, token.create
INSERT INTO role_permissions (role_id, permission, created_by, updated_by) VALUES
    ('11111111-1111-1111-1111-111111111111', 'token.create', 'Admin', 'Admin'),
    ('11111111-1111-1111-1111-111111111111', 'api.read', 'Admin', 'Admin'),
    ('11111111-1111-1111-1111-111111111111', 'api.write', 'Admin', 'Admin')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample permissions for ReadWrite role
INSERT INTO role_permissions (role_id, permission, created_by, updated_by) VALUES
    ('22222222-2222-2222-2222-222222222222', 'api.read', 'Admin', 'Admin'),
    ('22222222-2222-2222-2222-222222222222', 'api.write', 'Admin', 'Admin')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample permissions for ReadOnly role
INSERT INTO role_permissions (role_id, permission, created_by, updated_by) VALUES
    ('33333333-3333-3333-3333-333333333333', 'api.read', 'Admin', 'Admin')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample users with bcrypt password hashes
-- Note: These use predefined UUIDs for development. In production, use gen_random_uuid()
-- Default passwords (for development only):
--   AdminUser: adminsecret
--   ClientApp: clientsecret
-- 
-- Hashes generated with bcrypt work factor 12
INSERT INTO users (id, username, password_hash, created_by, updated_by) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Admin', '$2a$12$o/lEizsiyXbUjG5dSijR2OHUo7f6zjci179AXOyeYT5V.ii2C48gi', 'Admin', 'Admin'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ClientApp', '$2a$12$bEdsi67xom.wkxmPk6QvyO3/G0XtLqEHjMBNqn79UtakMWZOIQvFi', 'Admin', 'Admin')
ON CONFLICT (username) DO NOTHING;

-- Assign roles to users
INSERT INTO user_roles (user_id, role_id, created_by, updated_by) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Admin', 'Admin'), -- admin has Admin role
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222', 'Admin', 'Admin')  -- client has ReadWrite role
ON CONFLICT (user_id, role_id) DO NOTHING;

-- =============================================================================
-- COMMENTS ON TABLES
-- =============================================================================

COMMENT ON TABLE api_info IS 'Stores API version information, features, and status';
COMMENT ON TABLE roles IS 'Stores role definitions for authorization';
COMMENT ON TABLE role_permissions IS 'Stores permissions associated with each role';
COMMENT ON TABLE users IS 'Stores user accounts with bcrypt password hashes';
COMMENT ON TABLE user_roles IS 'Associates users with their assigned roles';
COMMENT ON TABLE action_audit IS 'Stores explicit business actions for audit trail and debugging';

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
-- Uncomment to verify the schema was created successfully:

-- SELECT 'API Info Count:' as check_name, COUNT(*) as result FROM api_info;
-- SELECT 'Roles Count:' as check_name, COUNT(*) as result FROM roles;
-- SELECT 'Users Count:' as check_name, COUNT(*) as result FROM users;
-- SELECT 'User Roles Count:' as check_name, COUNT(*) as result FROM user_roles;
-- SELECT 'Role Permissions Count:' as check_name, COUNT(*) as result FROM role_permissions;
-- SELECT 'Action Audit Count:' as check_name, COUNT(*) as result FROM action_audit;

-- =============================================================================
-- END OF SCHEMA
-- =============================================================================
