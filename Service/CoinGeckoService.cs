using Crypto.Data;
using Crypto.DTO;
using Crypto.Entities;
using Crypto.Interface;
using Microsoft.EntityFrameworkCore;

namespace Crypto.Service
{
    public class CoinGeckoService : ICoinGeckoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly string _url;
        private readonly string _apikey;

        public CoinGeckoService(HttpClient httpClient, IConfiguration config, AppDbContext db)
        {
            _httpClient = httpClient;
            _config = config;
            _db = db;
            _url = config["CoinGecko:BaseUrl"]!;
            _apikey = config["CoinGecko:ApiKey"]!;
        }

        public async Task AtualizarCotacoesAsync()
        {
            var moedas = await _db.Moedas.Where( m => m.Ativo).ToListAsync();

            var ids = string.Join(",", moedas.Select(m => m.CoinGeckoId));
            var baseUrl = _config["CoinGecko:BaseUrl"];
            var apiKey = _config["CoinGecko:ApiKey"]!;
            var url = $"{baseUrl}?vs_currencies=brl&ids={ids}&x_cg_demo_api_key={apiKey}";
            var resultado = await _httpClient.GetFromJsonAsync<Dictionary<string, CoinPriceDto>>(url);


            if(resultado == null) return;
            foreach(var moeda in moedas)
            {
                if(!resultado.TryGetValue(moeda.CoinGeckoId, out var preco)) continue;

                _db.Cotacoes.Add(new Cotacao
                {
                    Id = Guid.NewGuid(),
                    MoedaId = moeda.Id,
                    PrecoBrl = preco.Brl,
                    DataHora = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
            }
        }
    }
}