namespace Crypto.DTO
{
    public class OrdemMinhaResponseDto
    {
        public Guid Id { get; set; }
        public int MoedaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitarioBrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Papel { get; set; } = string.Empty; // Vendedor ou Comprador
        public DateTime DataHora { get; set; }
    }
}
