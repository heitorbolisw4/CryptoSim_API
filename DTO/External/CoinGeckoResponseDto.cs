using System.Text.Json.Serialization;

namespace Crypto.DTO
{

    public class CoinPriceDto
    {
        [JsonPropertyName("brl")]
        public decimal Brl { get; set; }
    }
}