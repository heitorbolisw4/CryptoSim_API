namespace Crypto.DTO
{
    public class CarteiraSaldoResponseDto
    {
        public decimal SaldoBrl { get; set; }
        public List<SaldoCriptoResponseDto> Saldos { get; set; } = new();
    }
}
