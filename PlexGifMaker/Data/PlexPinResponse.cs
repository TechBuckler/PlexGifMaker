using System.Text.Json.Serialization;

namespace PlexGifMaker.Data
{
    public class PlexPinResponse
    {
        // The ID of the PIN, used to check the status of the PIN later
        [JsonPropertyName("id")]
        public int Id { get; set; }

        // The PIN code that the user needs to enter on the Plex website to authorize the app
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        // The authentication token that will be available after the user authorizes the app
        // This will be null initially and will be populated once the PIN is approved/claimed
        [JsonPropertyName("authToken")]
        public string? AuthToken { get; set; }

        // Additional properties can be added here based on the API response
    }

}
