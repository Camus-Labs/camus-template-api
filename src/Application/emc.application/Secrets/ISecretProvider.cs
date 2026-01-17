namespace emc.camus.application.Secrets
{
    /// <summary>
    /// Provides methods for loading and retrieving application secrets from a secure store.
    /// </summary>
    public interface ISecretProvider
    {
        /// <summary>
        /// Loads the specified secrets asynchronously from the underlying secret store.
        /// </summary>
        /// <param name="secretNames">A collection of secret names to load.</param>
        /// <returns>A task representing the asynchronous load operation.</returns>
        Task LoadSecretsAsync(IEnumerable<string> secretNames);

        /// <summary>
        /// Retrieves the value of a loaded secret by name.
        /// </summary>
        /// <param name="name">The name of the secret to retrieve.</param>
        /// <returns>The secret value if found; otherwise, <c>null</c>.</returns>
        string? GetSecret(string name);

        /// <summary>
        /// Gets the number of secrets currently loaded in the provider.
        /// </summary>
        /// <returns>The count of loaded secrets.</returns>
        int GetLoadedSecretsCount();

        /// <summary>
        /// Determines whether a secret with the specified name is loaded.
        /// </summary>
        /// <param name="name">The name of the secret to check.</param>
        /// <returns><c>true</c> if the secret is loaded; otherwise, <c>false</c>.</returns>
        bool HasSecret(string name);

        /// <summary>
        /// Gets the names of all secrets currently loaded in the provider.
        /// </summary>
        /// <returns>An enumerable of loaded secret names.</returns>
        IEnumerable<string> GetLoadedSecretNames();
    }
}