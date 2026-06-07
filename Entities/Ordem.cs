namespace Crypto.Entities
{
    class Ordem
    {
        public Guid Id { get; set; }
        public Guid  CarteiraId { get; set; }
        public int MoedaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitarioBrl { get; set; }
        public string Status { get; set; } = string.Empty; // postada, em transacao, aprovada, cancelada
        public DateTime DataHora { get; set; }
    }
}