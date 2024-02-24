namespace rinha_backend_2401_01.Models;

public record SaldoExtrato
{
    public int Total { get; set; }
    public string DataExtrato { get; set; } = null!;
    public int Limite { get; set; }
}