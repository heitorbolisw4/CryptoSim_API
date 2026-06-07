namespace Crypto.Entities
{
    public class Cotacao
    {
        public Guid Id { get; set; }
        public int MoedaId { get; set; }
        public decimal PrecoBrl { get; set; }
        public DateTime DataHora { get; set; }

        public Moeda? Moeda { get; set; }
    }
}