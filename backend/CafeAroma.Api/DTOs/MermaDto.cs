namespace CafeAroma.Api.DTOs;

public class MermaDto
{
    public int InsumoId { get; set; }
    public decimal Cantidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}