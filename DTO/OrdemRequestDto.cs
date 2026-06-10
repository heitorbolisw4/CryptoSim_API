namespace Crypto.Dto
{
    public class OrdemRequestDto
    {
        public Guid CarteiraId { get; set; }
        public int MoedaId { get; set; }
        public string Tipo { get; set; } = string.Empty; // Compra, Venda
        public decimal Quantidade { get; set; }
    }
}