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
        /// <returns>The secret value.</returns>
        string GetSecret(string name);
    }
}