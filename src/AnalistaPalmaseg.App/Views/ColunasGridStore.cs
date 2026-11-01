using System.IO;
using System.Text.Json;
using System.Windows.Controls;

namespace AnalistaPalmaseg.App.Views;

/// <summary>
/// Persiste a largura das colunas de um DataGrid em %LocalAppData%\PalmaSeg\colunas-{chave}.json,
/// para que o ajuste feito pelo usuário sobreviva à troca de tela e ao fechamento do sistema.
/// </summary>
public static class ColunasGridStore
{
    private record Largura(double Valor, DataGridLengthUnitType Tipo);

    private static string Caminho(string chave) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PalmaSeg", $"colunas-{chave}.json");

    public static void Restaurar(DataGrid grid, string chave)
    {
        try
        {
            var arquivo = Caminho(chave);
            if (!File.Exists(arquivo)) return;
            var salvas = JsonSerializer.Deserialize<Dictionary<string, Largura>>(File.ReadAllText(arquivo));
            if (salvas == null) return;

            foreach (var coluna in grid.Columns)
            {
                if (coluna.Header is string h && salvas.TryGetValue(h, out var l) && l.Valor > 0)
                    coluna.Width = new DataGridLength(l.Valor, l.Tipo);
            }
        }
        catch
        {
            // Preferência de layout é opcional: arquivo ausente/corrompido mantém as larguras padrão.
        }
    }

    public static void Salvar(DataGrid grid, string chave)
    {
        try
        {
            var larguras = new Dictionary<string, Largura>();
            foreach (var coluna in grid.Columns)
            {
                if (coluna.Header is not string h) continue;
                larguras[h] = new Largura(coluna.Width.Value, coluna.Width.UnitType);
            }

            var arquivo = Caminho(chave);
            Directory.CreateDirectory(Path.GetDirectoryName(arquivo)!);
            File.WriteAllText(arquivo, JsonSerializer.Serialize(larguras));
        }
        catch (Exception ex)
        {
            // Falha ao gravar a preferência não deve interromper o uso da tela; registra o motivo para diagnóstico.
            try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "palmaseg-colunas-erro.log"), ex.ToString()); } catch { }
        }
    }
}
