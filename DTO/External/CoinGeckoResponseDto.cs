using System.Text.Json.Serialization;

namespace Crypto.DTO
{
    public class CoinGeckoResponseDto
    {
        public Dictionary<string, CoinPriceDto> Prices {get;set;}
    }

    public class CoinPriceDto
    {
        [JsonPropertyName("brl")]
        public decimal Brl { get; set; }
    }
}