namespace Crypto.Entities
{
    public class TransacaoFiat
    {
        public Guid Id { get; set; }
        public Guid? CarteiraId { get; set; }
        public Carteira? Carteira { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty; //pendente, concluido, cancelado
        public DateTime DataHora { get; set; }

    }
}