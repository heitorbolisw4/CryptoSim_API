namespace Crypto.Entities
{
    public class SaldoCripto
    {
        public int Id { get; set; }
        public int CarteiraId { get; set; }
        public Carteira? Carteira { get; set; }
        public int MoedaId { get; set; }
        public Moeda? Moeda { get; set; }
        public decimal Quantidade { get; set; }
    }
}