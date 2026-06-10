namespace Crypto.DTO
{
    public class TransacaoFiatResponseDto
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }
}
