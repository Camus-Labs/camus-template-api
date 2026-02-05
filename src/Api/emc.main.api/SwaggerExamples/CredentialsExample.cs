using Swashbuckle.AspNetCore.Filters;
using emc.camus.domain.Auth;

namespace emc.camus.main.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for Credentials in Swagger documentation.
    /// </summary>
    public class CredentialsExample : IExamplesProvider<Credentials>
    {
        /// <summary>
        /// Returns an example Credentials object for API documentation.
        /// </summary>
        /// <returns>Example credentials with sample AccessKey and AccessSecret.</returns>
        public Credentials GetExamples()
        {
            return new Credentials
            {
                AccessKey = $"TEST-{Guid.NewGuid()}",
                AccessSecret = $"SECRET-{Guid.NewGuid()}"
            };
        }
    }
}
