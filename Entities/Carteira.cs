namespace Crypto.Entities
{
    public class Carteira
    {
        public Guid Id { get; set; }
        public Guid? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public decimal SaldoBrl { get; set; }
        public DateTime DataCriacao { get; set; }

        public List<SaldoCripto>? Saldos { get; set; }
        public List<TransacaoFiat>? TransacaoFiat { get; set; }
        
    }
}