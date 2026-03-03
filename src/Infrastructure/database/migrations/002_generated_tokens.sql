-- =============================================================================
-- Camus PostgreSQL Database Schema - Generated Tokens Migration
-- =============================================================================
-- Migration: 002
-- Description: Add generated_tokens table for tracking custom tokens
-- Date: 2026-02-17
-- 
-- Usage:
--   psql -U postgres -d camus -f 002_generated_tokens.sql
--
-- Prerequisites:
--   - Migration 001_initial_schema.sql must be applied first
--   - PostgreSQL 12 or higher
-- =============================================================================

-- =============================================================================
-- GENERATED TOKENS TABLE
-- =============================================================================

-- Table: camus.generated_tokens
-- Stores custom generated tokens with permissions and tracking information
CREATE TABLE IF NOT EXISTS camus.generated_tokens (
    jti UUID PRIMARY KEY, -- JWT ID — primary identifier for the token
    creator_user_id UUID NOT NULL,
    creator_username VARCHAR(200) NOT NULL,
    token_username VARCHAR(221) NOT NULL, -- Original username + '-' + suffix (200 + 1 + 20)
    permissions TEXT[] NOT NULL, -- Array of permission strings
    expires_on TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMP,
    
    -- Constraints
    CONSTRAINT chk_permissions_not_empty CHECK (array_length(permissions, 1) > 0),
    CONSTRAINT chk_revoked_at_when_revoked CHECK (
        (is_revoked = TRUE AND revoked_at IS NOT NULL) OR 
        (is_revoked = FALSE AND revoked_at IS NULL)
    )
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_generated_tokens_creator_user_id ON camus.generated_tokens(creator_user_id);
CREATE INDEX IF NOT EXISTS idx_generated_tokens_expires_on ON camus.generated_tokens(expires_on);
CREATE INDEX IF NOT EXISTS idx_generated_tokens_is_revoked ON camus.generated_tokens(is_revoked) WHERE is_revoked = FALSE;

-- Comments for documentation
COMMENT ON TABLE camus.generated_tokens IS 'Stores custom generated tokens with specific permissions and expiration for audit and tracking';
COMMENT ON COLUMN camus.generated_tokens.creator_user_id IS 'User ID of the user who created this token';
COMMENT ON COLUMN camus.generated_tokens.creator_username IS 'Username of the user who created this token';
COMMENT ON COLUMN camus.generated_tokens.token_username IS 'Username associated with the token (includes suffix): {creator_username}-{suffix}';
COMMENT ON COLUMN camus.generated_tokens.jti IS 'JWT ID (JTI) — primary identifier for the generated token';
COMMENT ON COLUMN camus.generated_tokens.permissions IS 'Array of permission strings granted to this token';
COMMENT ON COLUMN camus.generated_tokens.expires_on IS 'Expiration date and time of the token (UTC)';
COMMENT ON COLUMN camus.generated_tokens.created_at IS 'Creation timestamp (UTC)';
COMMENT ON COLUMN camus.generated_tokens.is_revoked IS 'Flag indicating whether the token has been revoked';
COMMENT ON COLUMN camus.generated_tokens.revoked_at IS 'Timestamp when the token was revoked (UTC), NULL if not revoked';
