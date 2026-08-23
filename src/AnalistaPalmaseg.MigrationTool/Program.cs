using System.Globalization;
using Microsoft.Data.Sqlite;
using Npgsql;

if (args.Length < 2)
{
    Console.WriteLine("Uso: AnalistaPalmaseg.MigrationTool <caminho-dados.db> <connection-string-postgres>");
    Console.WriteLine("Ex.: AnalistaPalmaseg.MigrationTool C:\\Instalacao\\dados.db \"Host=localhost;Database=analista_palmaseg;Username=postgres;Password=postgres\"");
    return 1;
}

var sqlitePath = args[0];
var pgConnectionString = args[1];

if (!File.Exists(sqlitePath))
{
    Console.WriteLine($"Arquivo não encontrado: {sqlitePath}");
    return 1;
}

// Ordem de cópia respeitando as foreign keys (pais antes dos filhos).
string[] tables =
[
    "Usuarios",
    "Seguradoras",
    "Importacoes",
    "Renovacoes",
    "NovosNegocios",
    "Resultados",
    "FuncionariosResultados",
    "ImportacoesApolice",
    "Apolices",
    "RelatorioRenovacoes",
    "Anexos",
    "SeguroNovos",
    "MetasSeguradoras",
    "MetasPremiacao",
    "MetasCrescimento",
    "ValoresReferencia",
    "Clientes",
    "Leads",
    "DistribuicaoReferencias",
    "PastasSalvarPropostas",
];

await using var sqlite = new SqliteConnection($"Data Source={sqlitePath}");
await sqlite.OpenAsync();

await using var pg = new NpgsqlConnection(pgConnectionString);
await pg.OpenAsync();

var pgColumnTypes = await LoadPostgresColumnTypesAsync(pg);

foreach (var table in tables)
{
    if (!TableExists(sqlite, table))
    {
        Console.WriteLine($"[pular] tabela \"{table}\" não existe no dados.db de origem.");
        continue;
    }

    var copied = await CopyTableAsync(sqlite, pg, table, pgColumnTypes);
    Console.WriteLine($"[ok] {table}: {copied} linha(s) copiada(s).");
}

await ResetSequencesAsync(pg, tables);

Console.WriteLine("Migração de dados concluída.");
return 0;

static bool TableExists(SqliteConnection conn, string table)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name = $name";
    cmd.Parameters.AddWithValue("$name", table);
    return cmd.ExecuteScalar() is not null;
}

static async Task<Dictionary<string, Dictionary<string, string>>> LoadPostgresColumnTypesAsync(NpgsqlConnection pg)
{
    // tabela -> (coluna -> data_type)
    var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    await using var cmd = pg.CreateCommand();
    cmd.CommandText = "SELECT table_name, column_name, data_type FROM information_schema.columns WHERE table_schema = 'public'";
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var tableName = reader.GetString(0);
        var columnName = reader.GetString(1);
        var dataType = reader.GetString(2);

        if (!result.TryGetValue(tableName, out var columns))
        {
            columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            result[tableName] = columns;
        }
        columns[columnName] = dataType;
    }

    return result;
}

static async Task<int> CopyTableAsync(
    SqliteConnection sqlite,
    NpgsqlConnection pg,
    string table,
    Dictionary<string, Dictionary<string, string>> pgColumnTypes)
{
    if (!pgColumnTypes.TryGetValue(table, out var columnTypes))
    {
        Console.WriteLine($"[pular] tabela \"{table}\" não existe no Postgres de destino.");
        return 0;
    }

    await using var deleteCmd = pg.CreateCommand();
    deleteCmd.CommandText = $"DELETE FROM \"{table}\"";
    await deleteCmd.ExecuteNonQueryAsync();

    await using var selectCmd = sqlite.CreateCommand();
    selectCmd.CommandText = $"SELECT * FROM \"{table}\"";
    await using var reader = await selectCmd.ExecuteReaderAsync();

    var columnNames = Enumerable.Range(0, reader.FieldCount)
        .Select(reader.GetName)
        .Where(columnTypes.ContainsKey)
        .ToArray();

    var copied = 0;
    while (await reader.ReadAsync())
    {
        await using var insertCmd = pg.CreateCommand();
        var columnList = string.Join(", ", columnNames.Select(c => $"\"{c}\""));
        var paramList = string.Join(", ", columnNames.Select((_, i) => $"${i + 1}"));
        insertCmd.CommandText = $"INSERT INTO \"{table}\" ({columnList}) VALUES ({paramList})";

        foreach (var columnName in columnNames)
        {
            var rawValue = reader[columnName];
            var pgType = columnTypes[columnName];
            insertCmd.Parameters.Add(new NpgsqlParameter { Value = ConvertValue(rawValue, pgType) });
        }

        await insertCmd.ExecuteNonQueryAsync();
        copied++;
    }

    return copied;
}

static object ConvertValue(object rawValue, string pgDataType)
{
    if (rawValue is DBNull or null)
        return DBNull.Value;

    switch (pgDataType)
    {
        case "numeric":
            return rawValue switch
            {
                string s => decimal.Parse(s, CultureInfo.InvariantCulture),
                double d => (decimal)d,
                long l => (decimal)l,
                decimal dec => dec,
                _ => Convert.ToDecimal(rawValue, CultureInfo.InvariantCulture),
            };

        case "double precision":
        case "real":
            return Convert.ToDouble(rawValue, CultureInfo.InvariantCulture);

        case "boolean":
            return rawValue switch
            {
                long l => l != 0,
                bool b => b,
                _ => Convert.ToInt64(rawValue) != 0,
            };

        case "timestamp without time zone":
        case "timestamp with time zone":
        case "date":
            return rawValue switch
            {
                string s => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                DateTime dt => dt,
                _ => rawValue,
            };

        case "integer":
        case "bigint":
        case "smallint":
            return Convert.ToInt64(rawValue, CultureInfo.InvariantCulture);

        default:
            return rawValue;
    }
}

static async Task ResetSequencesAsync(NpgsqlConnection pg, IEnumerable<string> tables)
{
    foreach (var table in tables)
    {
        await using var cmd = pg.CreateCommand();
        cmd.CommandText = $"""
            SELECT setval(
                pg_get_serial_sequence('"{table}"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "{table}"), 0) + 1,
                false)
            """;
        try
        {
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException)
        {
            // Tabela sem coluna "Id" com sequence (ex.: tabelas sem PK autoincremento) — ignora.
        }
    }
}
