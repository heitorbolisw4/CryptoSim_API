namespace Crypto.Entities
{
    public class LogOperacao
    {
        public Guid Id { get; set; }
        public Guid UsuarioId { get; set; }
        public string Evento { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }
}