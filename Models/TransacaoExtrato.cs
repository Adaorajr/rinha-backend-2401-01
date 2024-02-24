namespace rinha_backend_2401_01.Models;

public record TransacaoExtrato
{
    public int Valor { get; set; }
    public string Tipo { get; set; } = null!;
    public string Descricao { get; set; } = null!;
    public string RealizadoEm { get; set; } = null!;
}