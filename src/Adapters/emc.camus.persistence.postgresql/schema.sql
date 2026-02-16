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
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
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
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
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
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
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
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
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
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
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
-- SAMPLE DATA (Optional - for development/testing)
-- =============================================================================

-- Sample API info data
-- Note: API info must match configuration in appsettings.json > AppDataSettings.InMemory.ApiInfos
INSERT INTO api_info (version, name, status, features) VALUES
    ('1.0', 'Camus DB API - 1.0 Release', 'Available', 
     ARRAY['Basic API Information', 'Public Endpoints', 'Basic Observability']),
    ('2.0', 'Camus DB API - 2.0 Beta', 'Beta', 
     ARRAY['Authentication (JWT & API Key)', 'Authorization (Role-based)', 'API Versioning', 
           'Observability (OpenTelemetry)', 'Rate Limiting (Multiple policies)', 'Swagger/OpenAPI Documentation',
           'Secret Management (Dapr)', 'Error Handling', 'CORS Support', 'Health Checks'])
ON CONFLICT (version) DO NOTHING;

-- Sample roles
-- Note: Role definitions must match configuration in appsettings.json > Authorization.InMemory.Roles
INSERT INTO roles (id, name, description) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Admin', 'Administrator with full access including token creation'),
    ('22222222-2222-2222-2222-222222222222', 'ReadWrite', 'Standard user with read and write access'),
    ('33333333-3333-3333-3333-333333333333', 'ReadOnly', 'User with read-only access')
ON CONFLICT (name) DO NOTHING;

-- Sample permissions for Admin role
-- Note: Permission values must match constants defined in emc.camus.application.Auth.Permissions class
--       Available permissions: api.read, api.write, token.create
INSERT INTO role_permissions (role_id, permission) VALUES
    ('11111111-1111-1111-1111-111111111111', 'token.create'),
    ('11111111-1111-1111-1111-111111111111', 'api.read'),
    ('11111111-1111-1111-1111-111111111111', 'api.write')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample permissions for ReadWrite role
INSERT INTO role_permissions (role_id, permission) VALUES
    ('22222222-2222-2222-2222-222222222222', 'api.read'),
    ('22222222-2222-2222-2222-222222222222', 'api.write')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample permissions for ReadOnly role
INSERT INTO role_permissions (role_id, permission) VALUES
    ('33333333-3333-3333-3333-333333333333', 'api.read')
ON CONFLICT (role_id, permission) DO NOTHING;

-- Sample users with bcrypt password hashes
-- Note: These use predefined UUIDs for development. In production, use gen_random_uuid()
-- Default passwords (for development only):
--   AdminUser: adminsecret
--   ClientApp: clientsecret
-- 
-- Hashes generated with bcrypt work factor 12
INSERT INTO users (id, username, password_hash) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'AdminUser', '$2a$12$o/lEizsiyXbUjG5dSijR2OHUo7f6zjci179AXOyeYT5V.ii2C48gi'),
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ClientApp', '$2a$12$bEdsi67xom.wkxmPk6QvyO3/G0XtLqEHjMBNqn79UtakMWZOIQvFi')
ON CONFLICT (username) DO NOTHING;

-- Assign roles to users
INSERT INTO user_roles (user_id, role_id) VALUES
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111'), -- admin has Admin role
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222')  -- client has ReadWrite role
ON CONFLICT (user_id, role_id) DO NOTHING;

-- =============================================================================
-- COMMENTS ON TABLES
-- =============================================================================

COMMENT ON TABLE api_info IS 'Stores API version information, features, and status';
COMMENT ON TABLE roles IS 'Stores role definitions for authorization';
COMMENT ON TABLE role_permissions IS 'Stores permissions associated with each role';
COMMENT ON TABLE users IS 'Stores user accounts with bcrypt password hashes';
COMMENT ON TABLE user_roles IS 'Associates users with their assigned roles';

-- =============================================================================
-- VERIFICATION QUERIES
-- =============================================================================
-- Uncomment to verify the schema was created successfully:

-- SELECT 'API Info Count:' as check_name, COUNT(*) as result FROM api_info;
-- SELECT 'Roles Count:' as check_name, COUNT(*) as result FROM roles;
-- SELECT 'Users Count:' as check_name, COUNT(*) as result FROM users;
-- SELECT 'User Roles Count:' as check_name, COUNT(*) as result FROM user_roles;
-- SELECT 'Role Permissions Count:' as check_name, COUNT(*) as result FROM role_permissions;

-- =============================================================================
-- END OF SCHEMA
-- =============================================================================
