# GitHub Copilot Instructions for C# Web API Development


## Project Context
**AI Role**: Maintain consistent, standards-based code across all project files

## 🚨 CRITICAL ENFORCEMENT RULES


### 1. NAMING CONVENTIONS (MANDATORY)
| Type | Convention | Examples |
|------|------------|----------|
| **Constants**         | `UPPER_SNAKE_CASE` | `MaxItems`, `DEFAULT_TIMEOUT` |
| **Class Names**       | `PascalCase`                       | `AuthController`, `ApiInfo` |
| **Method Names**      | `PascalCase`                       | `GetInfo`, `CreateToken` |
| **Property Names**    | `PascalCase`                       | `AccessKey`, `ExpiresOn` |
| **Field Names**       | `_camelCase` (private), `PascalCase` (public) | `_logger`, `status` |
| **Parameter Names**   | `camelCase`                        | `accessKey`, `request` |
| **Local Variables**   | `camelCase`                        | `token`, `userId` |
| **Interface Names**   | `IPascalCase`                      | `ILogger`, `IService` |
| **Enum Names**        | `PascalCase`                       | `StatusCode`, `LogLevel` |
| **Enum Members**      | `PascalCase`                       | `Success`, `Error` |
| **Namespace Names**   | `PascalCase`                       | `Emc.Camus.Main.Api` |
| **File Names**        | `PascalCase`                       | `AuthController.cs`, `Program.cs` |
| **JSON Output**       | `camelCase`                        | `accessKey`, `statusCode` |


### 2. DOCUMENTATION STANDARDS (ALWAYS REQUIRED)


#### C# File/Class Header (Required at top of every public class/file):
```csharp
/// <summary>
/// <FILE OR CLASS NAME>
/// </summary>
/// <remarks>
/// Purpose: <WHAT THE CLASS OR FILE DOES>
/// Usage: <HOW TO USE THE CLASS>
/// Output: <WHAT THE CLASS PRODUCES>
/// Dependencies: <REQUIRED SERVICES/MODULES>
/// </remarks>
```

#### Method/Action Header (Required above every public method/action):
```csharp
/// <summary>
/// <IMPERATIVE SUMMARY OF WHAT THE METHOD DOES>
/// </summary>
/// <param name="paramName">Description of parameter</param>
/// <returns>Description of return value</returns>
```

#### Required Swagger Annotations (for all public API actions):

```csharp
[SwaggerOperation(
    Description = "Detailed description for API consumers."
)]
[ProducesResponseType(typeof(ReturnType), StatusCodes.Status200OK)]
// Only include the following if the endpoint can actually return these status codes:
[ProducesResponseType(StatusCodes.Status400BadRequest)] // If input can be invalid
[ProducesResponseType(StatusCodes.Status401Unauthorized)] // If endpoint requires authentication
[ProducesResponseType(StatusCodes.Status403Forbidden)] // If endpoint can be forbidden
[ProducesResponseType(StatusCodes.Status404NotFound)] // If resource may not be found
[ProducesResponseType(StatusCodes.Status500InternalServerError)] // For unhandled errors
```

> Only include `[ProducesResponseType]` for status codes that are realistically possible for the given endpoint. For example, only add `StatusCodes.Status400BadRequest` if the action accepts input that could be invalid (e.g., query, route, or body parameters).

**Checklist:**
- All public controllers, models, and methods must have XML documentation comments.
- All public API actions must use `[SwaggerOperation]` and `[ProducesResponseType]` for all expected status codes.
- Only applicable status codes are documented for each endpoint (do not add 400/401/403/404 if not possible).
- Use `<remarks>` for extended documentation when needed.


### 3. ERROR HANDLING PATTERN


#### Structure (MANDATORY for Public API Actions):
```csharp
try
{
    // Main logic
    // ...
    return Ok(result); // or other appropriate response
}
catch (Exception ex)
{
    // Handle unexpected server errors
    _logger.LogError(ex, "Internal server error: {Message}", ex.Message);
    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." });
}
```

**Guidelines:**
- Always use try-catch in public API actions to handle and log errors.
- Never expose sensitive exception details to clients—log full details, return generic messages.
- Use structured error responses (e.g., `{ error = "message" }`).


### 6. CRITICAL VALIDATION CHECKLIST


Before completing any C# Web API code changes:
- [ ] All naming conventions are followed (see Naming Conventions section)
- [ ] XML documentation header present for every public class and method
- [ ] Swagger annotations (`[SwaggerOperation]`, `[ProducesResponseType]`) present for all public API actions
- [ ] Error handling uses try-catch in all public API actions
- [ ] No sensitive exception details exposed to clients
- [ ] All API responses use structured objects (not plain strings)
- [ ] All controllers, models, and DTOs use PascalCase for public members
- [ ] All JSON output uses camelCase (unless external contract requires otherwise)
- [ ] No magic numbers or strings—use constants or enums
- [ ] All dependencies injected via constructor (no service locator or static access)


---
**File Purpose**: Ensure consistent, maintainable C# code