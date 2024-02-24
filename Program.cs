using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using rinha_backend_2401_01.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Rinha")!
);

var app = builder.Build();

int[] clientes = [1, 2, 3, 4, 5];

app.MapPost("/clientes/{id}/transacoes", async (
    [FromRoute] int id,
    [FromBody] Transacao transacao,
    [FromServices] NpgsqlConnection con) =>
{
    if (!clientes.Any(x => x == id))
    {
        return Results.NotFound();
    }

    if (!transacao.EhValida())
    {
        return Results.UnprocessableEntity();
    }

    (int saldo, int limite, string mensagem) = await CriarTransacao(id, transacao, con);

    if (saldo == -1)
    {
        return Results.UnprocessableEntity();
    }

    return Results.Ok(new { saldo, limite });
});

static async Task<(int, int, string)> CriarTransacao(int clienteId, Transacao transacao, NpgsqlConnection con)
{
    await con.OpenAsync();
    using NpgsqlTransaction transaction = await con.BeginTransactionAsync();

    var saldo = await con.QuerySingleAsync<Saldo>(
         "select limite, valor from public.saldos where cliente_id = @ClienteId for update;",
         new { ClienteId = clienteId });

    const string sql =
        @"update public.saldos set valor = @ValorSaldo
         where cliente_id = @ClienteId;

         insert into public.transacao (valor, tipo, descricao, cliente_id)
         values (@ValorTransacao, @Tipo, @Descricao, @ClienteId);";

    if (transacao.Tipo == "c")
    {
        saldo.Valor += transacao.Valor;
    }

    if (transacao.Tipo == "d")
    {
        if (saldo.Valor - transacao.Valor >= saldo.Limite * -1)
        {
            saldo.Valor -= transacao.Valor;
        }
        else
        {
            return (-1, saldo.Limite, "Saldo insuficiente");
        }
    }

    await con.ExecuteAsync(
       sql,
       new
       {
           ValorTransacao = transacao.Valor,
           transacao.Tipo,
           transacao.Descricao,
           ClienteId = clienteId,
           ValorSaldo = saldo.Valor
       });

    await transaction.CommitAsync();

    return (saldo.Valor, saldo.Limite, "sucesso");
}

app.MapGet("/clientes/{id}/extrato", async (
    int id,
    [FromServices] NpgsqlConnection con) =>
{
    if (!clientes.Any(x => x == id))
    {
        return Results.NotFound();
    }

    await con.OpenAsync();

    const string sql =
        @"select
        	valor as Total,
        	timezone('utc',	now()) as DataExtrato,
        	limite
        from
        	public.saldos
        where
        	cliente_id = @id;

        select
        	valor,
        	tipo,
        	descricao,
        	realizada_em AS RealizadoEm
        from
        	public.transacao
        where
        	cliente_id = @id
        order by
        	realizada_em desc;";

    using var multi = await con.QueryMultipleAsync(sql, new { id });

    SaldoExtrato saldo = await multi.ReadSingleAsync<SaldoExtrato>();
    IEnumerable<TransacaoExtrato> transacoes = await multi.ReadAsync<TransacaoExtrato>();

    return Results.Ok(new { saldo, transacoes });
});

app.Run();