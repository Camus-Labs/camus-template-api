using Npgsql;

namespace emc.camus.api.integration.test.Helpers;

/// <summary>
/// Re-seeds the PostgreSQL database with reference data after a Respawn reset.
/// Mirrors the sample data from the initial migration script (001_initial_schema.sql).
/// </summary>
internal static class DatabaseSeeder
{
    private const string SeedSql = """
        INSERT INTO camus.api_info (version, name, status, features)
        VALUES
            ('1.0', 'Camus DB API - 1.0 Release', 'Available',
             ARRAY['Basic API Information', 'Public Endpoints', 'Basic Observability']),
            ('2.0', 'Camus DB API - 2.0 Beta', 'Beta',
             ARRAY['Authentication (JWT & API Key)', 'Authorization (Role-based)', 'API Versioning',
                   'Observability (OpenTelemetry)', 'Rate Limiting (Multiple policies)', 'Swagger/OpenAPI Documentation',
                   'Secret Management (Dapr)', 'Error Handling', 'CORS Support', 'Health Checks']);

        INSERT INTO camus.roles (id, name, description)
        VALUES
            ('11111111-1111-1111-1111-111111111111', 'Admin', 'Administrator with full access including token creation'),
            ('22222222-2222-2222-2222-222222222222', 'ReadWrite', 'Standard user with read and write access'),
            ('33333333-3333-3333-3333-333333333333', 'ReadOnly', 'User with read-only access');

        INSERT INTO camus.role_permissions (role_id, permission)
        VALUES
            ('11111111-1111-1111-1111-111111111111', 'token.create'),
            ('11111111-1111-1111-1111-111111111111', 'api.read'),
            ('11111111-1111-1111-1111-111111111111', 'api.write'),
            ('22222222-2222-2222-2222-222222222222', 'api.read'),
            ('22222222-2222-2222-2222-222222222222', 'api.write'),
            ('33333333-3333-3333-3333-333333333333', 'api.read');

        INSERT INTO camus.users (id, username, password_hash)
        VALUES
            ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Admin',
             '$2a$12$o/lEizsiyXbUjG5dSijR2OHUo7f6zjci179AXOyeYT5V.ii2C48gi'),
            ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'ClientApp',
             '$2a$12$bEdsi67xom.wkxmPk6QvyO3/G0XtLqEHjMBNqn79UtakMWZOIQvFi');

        INSERT INTO camus.user_roles (user_id, role_id)
        VALUES
            ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111'),
            ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '22222222-2222-2222-2222-222222222222');
        """;

    /// <summary>
    /// Inserts reference data (api_info, roles, permissions, users, user-role assignments)
    /// into the database. Must be called on an already-open connection.
    /// </summary>
    public static async Task SeedAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand(SeedSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
