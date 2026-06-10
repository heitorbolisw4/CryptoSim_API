namespace Crypto.DTO
{
    public class SaldoCriptoResponseDto
    {
        public int MoedaId { get; set; }
        public string Simbolo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }
}
