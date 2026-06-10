namespace Crypto.DTO
{
    public class MoedaResponseDto
    {
        public int Id { get; set; }
        public string Simbolo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal PrecoBrl { get; set; }
    }
}