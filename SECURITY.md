# Security Policy

## Security Best Practices

When deploying this template, please follow these security best practices:

### Authentication & Secrets

- **Never commit secrets**: Do not commit API keys, JWT signing keys, or connection strings to version control
- **Use secret management**: Utilize Azure Key Vault, AWS Secrets Manager, or Dapr secret stores for production
- **Rotate credentials**: Regularly rotate API keys, certificates, and database passwords
- **Strong signing keys**: Use RSA 2048-bit or higher for JWT signing

### Configuration

- **Environment variables**: Use environment variables for sensitive configuration in production
- **HTTPS only**: Always use HTTPS in production (never HTTP)
- **Rate limiting**: Enable and configure rate limiting to prevent abuse
- **CORS policies**: Configure restrictive CORS policies for production

### Deployment

- **Container security**: Run containers as non-root users (already configured in Dockerfile)
- **Network isolation**: Use network policies to restrict container communication
- **Regular updates**: Keep .NET runtime and dependencies up to date
- **Security headers**: Ensure HSTS, CSP, and other security headers are enabled

### Monitoring

- **Log security events**: Monitor authentication failures, rate limit violations, and suspicious activity
- **Alert on anomalies**: Set up alerts for unusual patterns
- **Audit trails**: Maintain audit logs for sensitive operations

## Known Security Considerations

### JWT Token Security

- Tokens are signed with RSA256 (asymmetric signing)
- Token expiration is configurable (default: 60 minutes)
- Consider implementing token refresh mechanism for long-lived sessions
- Store signing keys securely, never in application settings

### API Key Authentication

- API keys are validated against secure secret provider
- Consider implementing key rotation mechanism
- Monitor API key usage for anomalies

### Rate Limiting

- IP-based sliding window algorithm with configurable segments for smooth distribution
- Three fixed policies: `default` (250/min), `strict` (50/min), `relaxed` (500/min)
- Runs before authentication — all requests are rate-limited
- Returns 429 with `Retry-After` header when limit exceeded
- Health, readiness, and Swagger paths are exempt by default
- Customize per environment via `RateLimitingSettings` in `appsettings.json`:

```json
{
  "RateLimitingSettings": {
    "SegmentsPerWindow": 5,
    "DefaultPermitLimit": 250,
    "DefaultWindowSeconds": 60,
    "StrictPermitLimit": 50,
    "StrictWindowSeconds": 60,
    "RelaxedPermitLimit": 500,
    "RelaxedWindowSeconds": 60,
    "ExemptPaths": ["/health", "/ready", "/alive", "/swagger"]
  }
}
```

Apply policies to endpoints using the `[RateLimit]` attribute with policy name constants
from `RateLimitPolicies` (`Default`, `Strict`, `Relaxed`).

## Security Updates

We will notify users of security updates through:

- GitHub releases and tags
- CHANGELOG.md updates
- Email notifications (if you've opted in)

## Attribution

We appreciate responsible disclosure and will acknowledge security researchers who report vulnerabilities
(with their permission).
