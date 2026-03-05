using Swashbuckle.AspNetCore.Filters;
using emc.camus.api.Models.Requests.V2;

namespace emc.camus.api.SwaggerExamples.V2;

/// <summary>
/// Provides example data for AuthenticateUserRequest in Swagger documentation.
/// </summary>
public class AuthenticateUserRequestExample
    : IExamplesProvider<AuthenticateUserRequest>
{
    /// <summary>
    /// Returns an example AuthenticateUserRequest for API documentation.
    /// </summary>
    /// <returns>Example authentication request with sample credentials.</returns>
    public AuthenticateUserRequest GetExamples()
    {
        return new AuthenticateUserRequest
        {
            Username = $"testuser-{Guid.NewGuid()}",
            Password = $"password-{Guid.NewGuid()}"
        };
    }
}
