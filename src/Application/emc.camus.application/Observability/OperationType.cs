namespace emc.camus.application.Observability
{
    /// <summary>
    /// Enum for allowed operation types in telemetry.
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Read operation - retrieving data.
        /// </summary>
        Read,
        
        /// <summary>
        /// Authentication operation - login, token generation, validation.
        /// </summary>
        Auth,
        
        /// <summary>
        /// Info operation - retrieving informational data.
        /// </summary>
        Info,
        
        /// <summary>
        /// Create operation - creating new resources.
        /// </summary>
        Create,
        
        /// <summary>
        /// Update operation - modifying existing resources.
        /// </summary>
        Update,
        
        /// <summary>
        /// Delete operation - removing resources.
        /// </summary>
        Delete,
        // Add more as needed
    }
}
