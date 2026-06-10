namespace Crypto.DTO
{
    public class CarteiraSaldoResponseDto
    {
        public Guid Id { get; set; }
        public decimal SaldoBrl { get; set; }
        public List<SaldoCriptoResponseDto> Saldos { get; set; } = new();
    }
}
