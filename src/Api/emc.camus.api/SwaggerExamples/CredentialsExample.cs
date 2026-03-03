using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Requests.V2;

namespace emc.camus.api.SwaggerExamples
{
    /// <summary>
    /// Provides example data for AuthenticateUserRequest in Swagger documentation.
    /// </summary>
    public class CredentialsExample : IExamplesProvider<AuthenticateUserRequest>
    {
        /// <summary>
        /// Returns an example AuthenticateUserRequest object for API documentation.
        /// </summary>
        /// <returns>Example authentication request with sample Username and Password.</returns>
        public AuthenticateUserRequest GetExamples()
        {
            return new AuthenticateUserRequest
            {
                Username = $"testuser-{Guid.NewGuid()}",
                Password = $"password-{Guid.NewGuid()}"
            };
        }
    }
}
