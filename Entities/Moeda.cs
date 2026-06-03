namespace Crypto.Entities
{
    public class Moeda
    {
        public int Id { get; set; }
        public string Simbolo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public SaldoCripto? SaldoCripto { get; set; }
        public bool Ativo { get; set; }
    }
}