namespace Crypto.DTO
{
    public class OrdemResponseDto
    {
        public Guid Id { get; set; }
        public Guid CarteiraId { get; set; }
        public int MoedaId { get; set; }
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitarioBrl { get; set; }
        public DateTime DataHora { get; set; }
    }
}
