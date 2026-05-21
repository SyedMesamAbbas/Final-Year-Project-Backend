using System.Text.Json;
namespace HouseofTutorAPI.Services
{
    public class GeoService
    {
        private readonly HttpClient _httpClient;

        public GeoService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            // 🔥 REQUIRED HEADER (without this = address not found)
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HouseOfTutorApp/1.0");
        }

        public async Task<string> GetAddressAsync(double lat, double lon)
        {
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={lat}&lon={lon}&format=json";

            var response = await _httpClient.GetAsync(url);

            var json = await response.Content.ReadAsStringAsync();

            // 🔥 PRINT RAW RESPONSE (VERY IMPORTANT)
            Console.WriteLine("===== NOMINATIM RESPONSE =====");
            Console.WriteLine(json);

            using var doc = JsonDocument.Parse(json);

            // ✅ FIXED parsing
            if (doc.RootElement.TryGetProperty("display_name", out JsonElement display))
            {
                return display.GetString();
            }

            return null;
        }
    }
}