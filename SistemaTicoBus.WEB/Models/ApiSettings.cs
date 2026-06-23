namespace SistemaTicoBus.WEB.Models
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string HeaderName { get; set; } = "X-API-KEY";
        public string ApiKey { get; set; } = string.Empty;
    }
}