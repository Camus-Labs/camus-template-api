namespace emc.camus.main.api.Models
{
    public class JwtTokenRequest
    {
        public string? AccessKey { get; set; }
        public string? AccessSecret { get; set; }
    }
    
    public class JwtTokenResponse
    {
        public string? Token { get; set; }
        public DateTime ExpiresOn { get; set; }
    }
    
    public class ApiInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}