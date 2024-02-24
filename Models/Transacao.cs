namespace rinha_backend_2401_01.Models;

public record Transacao(int Valor, string Tipo, string Descricao)
{
    private readonly static string[] TipoTransacao = ["c", "d"];

    public bool EhValida()
    {
        return Valor > 0
            && !string.IsNullOrWhiteSpace(Descricao)
            && Descricao.Length <= 10
            && TipoTransacao.Contains(Tipo);
    }
}