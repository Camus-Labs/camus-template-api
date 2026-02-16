namespace emc.camus.persistence.postgresql.Models;

/// <summary>
/// Data model representing api_info table structure in PostgreSQL.
/// Used by Dapper for ORM mapping.
/// </summary>
public class ApiInfoModel
{
    /// <summary>
    /// Gets or sets the API name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API status (e.g., "active", "deprecated").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of features available in this API version.
    /// Maps to PostgreSQL TEXT[] array type.
    /// </summary>
    public List<string>? Features { get; set; }
}
