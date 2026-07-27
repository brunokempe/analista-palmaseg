using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.Core.Services;

public class FolhaAmarelaService
{
    private static readonly string[] DiaPt = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];
    private static readonly CultureInfo PtBr = new("pt-BR");

    public Task GerarLoteAsync(
        IReadOnlyList<(RelatorioRenovacao Registro, List<Anexo> Anexos)> itens,
        string diretorioBase,
        IProgress<(int Atual, int Total)> progresso)
    {
        return Task.Run(() =>
        {
            var template = LocalizarTemplate();
            for (int i = 0; i < itens.Count; i++)
            {
                var (reg, anexos) = itens[i];
                progresso.Report((i + 1, itens.Count));

                var nomePasta = Sanitizar($"{reg.DocumentoPrincipal}_{reg.NomeCliente}_{reg.Proposta}");
                var pasta = Path.Combine(diretorioBase, nomePasta);
                Directory.CreateDirectory(pasta);

                var pathFolha = Path.Combine(pasta,
                    $"FolhaAmarela_{Sanitizar(reg.NomeCliente ?? reg.Proposta ?? "Registro")}.odt");
                if (template != null)
                    GerarComTemplate(template, pathFolha, reg);
                else
                    GerarDoZero(pathFolha, reg);

                AnexoService.CopiarParaDiretorio(anexos, pasta);
            }
        });
    }

    public string GerarEAbrir(RelatorioRenovacao reg)
    {
        var path = Gerar(reg);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        return path;
    }

    public string Gerar(RelatorioRenovacao reg)
    {
        var nome = Sanitizar(reg.NomeCliente ?? "Folha");
        var pasta = AnexoService.ObterPasta(reg.Id);
        var path = Path.Combine(pasta, $"FolhaAmarela_{nome}.odt");

        if (File.Exists(path)) return path;

        var template = LocalizarTemplate();
        if (template != null)
            GerarComTemplate(template, path, reg);
        else
            GerarDoZero(path, reg);

        return path;
    }

    private static string? LocalizarTemplate()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnalistaPalmaseg", "FolhaTemplate.odt");
        if (File.Exists(appData)) return appData;

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var dir in new[]
        {
            Path.Combine(profile, "Downloads", "Desktop"),
            Path.Combine(profile, "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        })
        {
            if (!Directory.Exists(dir)) continue;
            var file = Directory.GetFiles(dir, "*Folha amarela*.odt", SearchOption.TopDirectoryOnly)
                                .FirstOrDefault();
            if (file == null) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(appData)!);
            File.Copy(file, appData, overwrite: false);
            return appData;
        }

        return null;
    }

    private static void GerarComTemplate(string templatePath, string outputPath, RelatorioRenovacao reg)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.Copy(templatePath, outputPath, overwrite: true);
        using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Update);

        zip.GetEntry("content.xml")?.Delete();
        var content = zip.CreateEntry("content.xml", CompressionLevel.Optimal);
        using (var w = new StreamWriter(content.Open(), new UTF8Encoding(false)))
            w.Write(ContentXml(reg));

        zip.GetEntry("styles.xml")?.Delete();
        var styles = zip.CreateEntry("styles.xml", CompressionLevel.Optimal);
        using (var w = new StreamWriter(styles.Open(), new UTF8Encoding(false)))
            w.Write(StylesXml());
    }

    private static void GerarDoZero(string path, RelatorioRenovacao reg)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        var mime = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using (var w = new StreamWriter(mime.Open(), new UTF8Encoding(false)))
            w.Write("application/vnd.oasis.opendocument.text");
        AddText(zip, "META-INF/manifest.xml", Manifest());
        AddText(zip, "meta.xml", MetaXml());
        AddText(zip, "styles.xml", StylesXml());
        AddText(zip, "content.xml", ContentXml(reg));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void AddText(ZipArchive zip, string name, string content)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(content);
    }

    private static string Sanitizar(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        if (s.Length > 40) s = s[..40];
        // Windows não permite nomes de pasta/arquivo com espaço ou ponto no final
        return s.TrimEnd(' ', '.');
    }

    private static string X(string? s) =>
        (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FormatData(DateTime? d) =>
        d == null ? "" : $"{DiaPt[(int)d.Value.DayOfWeek]} {d.Value:dd/MM/yy}";

    private static string Idade(DateTime? nasc)
    {
        if (nasc == null) return "";
        var hoje = DateTime.Today;
        int anos = hoje.Year - nasc.Value.Year;
        int meses = hoje.Month - nasc.Value.Month;
        if (hoje.Day < nasc.Value.Day) meses--;
        if (meses < 0) { anos--; meses += 12; }
        return $"({anos}a{meses}m)";
    }

    private static string Moeda(decimal v) => v.ToString("N2", PtBr);

    private static string Juntar(params string?[] partes) =>
        string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));

    // ── ODT structure ─────────────────────────────────────────────────────────

    private static string Manifest() => """
        <?xml version="1.0" encoding="UTF-8"?>
        <manifest:manifest xmlns:manifest="urn:oasis:names:tc:opendocument:xmlns:manifest:1.0" manifest:version="1.3">
          <manifest:file-entry manifest:full-path="/" manifest:version="1.3" manifest:media-type="application/vnd.oasis.opendocument.text"/>
          <manifest:file-entry manifest:full-path="content.xml" manifest:media-type="text/xml"/>
          <manifest:file-entry manifest:full-path="styles.xml" manifest:media-type="text/xml"/>
          <manifest:file-entry manifest:full-path="meta.xml" manifest:media-type="text/xml"/>
        </manifest:manifest>
        """;

    private static string MetaXml() => """
        <?xml version="1.0" encoding="UTF-8"?>
        <office:document-meta
          xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
          xmlns:meta="urn:oasis:names:tc:opendocument:xmlns:meta:1.0"
          office:version="1.4">
          <office:meta>
            <meta:generator>AnalistaPalmaseg</meta:generator>
          </office:meta>
        </office:document-meta>
        """;

    private static string StylesXml() => """
        <?xml version="1.0" encoding="UTF-8"?>
        <office:document-styles
          xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
          xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
          xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
          xmlns:fo="urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"
          xmlns:svg="urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0"
          office:version="1.4">
          <office:font-face-decls>
            <style:font-face style:name="Lato" svg:font-family="Lato"/>
            <style:font-face style:name="Liberation Serif" svg:font-family="'Liberation Serif'" style:font-family-generic="roman" style:font-pitch="variable"/>
          </office:font-face-decls>
          <office:styles>
            <style:style style:name="Standard" style:family="paragraph" style:class="text">
              <style:text-properties style:font-name="Liberation Serif" fo:font-size="12pt"/>
            </style:style>
            <style:style style:name="Text_20_body" style:display-name="Text body" style:family="paragraph" style:parent-style-name="Standard" style:class="text">
              <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0.247cm"/>
            </style:style>
            <style:style style:name="Table_20_Contents" style:display-name="Table Contents" style:family="paragraph" style:parent-style-name="Standard" style:class="extra">
              <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm"/>
            </style:style>
            <style:style style:name="Horizontal_20_Line" style:display-name="Horizontal Line" style:family="paragraph" style:parent-style-name="Standard" style:class="html">
              <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0.247cm"
                fo:padding-bottom="0.002cm"
                fo:border-bottom="0.88pt solid #000000"
                fo:border-left="none" fo:border-right="none" fo:border-top="none"/>
            </style:style>
          </office:styles>
          <office:automatic-styles>
            <style:page-layout style:name="pm1">
              <style:page-layout-properties fo:page-width="21cm" fo:page-height="29.7cm"
                style:print-orientation="portrait"
                fo:margin-top="0.8cm" fo:margin-bottom="0.8cm"
                fo:margin-left="1cm" fo:margin-right="1cm"
                fo:background-color="#FFFF99"/>
            </style:page-layout>
          </office:automatic-styles>
          <office:master-styles>
            <style:master-page style:name="Standard" style:page-layout-name="pm1"/>
          </office:master-styles>
        </office:document-styles>
        """;

    private static string ContentXml(RelatorioRenovacao r)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
        sb.AppendLine("""
            <office:document-content
              xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
              xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0"
              xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
              xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
              xmlns:fo="urn:oasis:names:tc:opendocument:xmlns:xsl-fo-compatible:1.0"
              xmlns:svg="urn:oasis:names:tc:opendocument:xmlns:svg-compatible:1.0"
              office:version="1.4">
            """);

        sb.AppendLine("""
              <office:font-face-decls>
                <style:font-face style:name="Lato" svg:font-family="Lato"/>
              </office:font-face-decls>
            """);

        sb.AppendLine("  <office:automatic-styles>");
        sb.AppendLine(AutoStyles());
        sb.AppendLine("  </office:automatic-styles>");

        sb.AppendLine("  <office:body>");
        sb.AppendLine("    <office:text>");
        sb.AppendLine(Tabela(r));

        // Observação
        var obs = Juntar(r.ObservacaoDocumento, r.Observacao);
        if (!string.IsNullOrWhiteSpace(obs))
        {
            var linhas = obs.Split('\n');
            sb.Append("""    <text:p text:style-name="P25">""");
            for (int i = 0; i < linhas.Length; i++)
            {
                if (i > 0) sb.Append("<text:line-break/>");
                sb.Append(X(linhas[i].Trim()));
            }
            sb.AppendLine("</text:p>");
        }
        else
        {
            sb.AppendLine("""    <text:p text:style-name="P25"/>""");
        }

        sb.AppendLine("""    <text:p text:style-name="P26"><text:span>LCTO. </text:span><text:span>P</text:span><text:span>0E1 R2N3 A4M5 B6U7 C8O9</text:span></text:p>""");
        sb.AppendLine("    </office:text>");
        sb.AppendLine("  </office:body>");
        sb.AppendLine("</office:document-content>");
        return sb.ToString();
    }

    private static string AutoStyles() => """
        <style:style style:name="Tabela1" style:family="table" style:master-page-name="Standard">
          <style:table-properties style:width="19cm" style:page-number="1" table:align="margins"/>
        </style:style>
        <style:style style:name="Tabela1.A" style:family="table-column"><style:table-column-properties style:column-width="3.6cm"/></style:style>
        <style:style style:name="Tabela1.B" style:family="table-column"><style:table-column-properties style:column-width="0.9cm"/></style:style>
        <style:style style:name="Tabela1.C" style:family="table-column"><style:table-column-properties style:column-width="2.7cm"/></style:style>
        <style:style style:name="Tabela1.D" style:family="table-column"><style:table-column-properties style:column-width="1.8cm"/></style:style>
        <style:style style:name="Tabela1.E" style:family="table-column"><style:table-column-properties style:column-width="0.4cm"/></style:style>
        <style:style style:name="Tabela1.F" style:family="table-column"><style:table-column-properties style:column-width="1.35cm"/></style:style>
        <style:style style:name="Tabela1.G" style:family="table-column"><style:table-column-properties style:column-width="0.28cm"/></style:style>
        <style:style style:name="Tabela1.H" style:family="table-column"><style:table-column-properties style:column-width="0.5cm"/></style:style>
        <style:style style:name="Tabela1.I" style:family="table-column"><style:table-column-properties style:column-width="0.07cm"/></style:style>
        <style:style style:name="Tabela1.J" style:family="table-column"><style:table-column-properties style:column-width="2.48cm"/></style:style>
        <style:style style:name="Tabela1.K" style:family="table-column"><style:table-column-properties style:column-width="3.93cm"/></style:style>
        <style:style style:name="Tabela1.A1" style:family="table-cell">
          <style:table-cell-properties fo:padding="0.097cm" fo:border="none" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.I1" style:family="table-cell">
          <style:table-cell-properties fo:padding="0.097cm" fo:border-left="none" fo:border-right="none" fo:border-top="none" fo:border-bottom="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.I2" style:family="table-cell">
          <style:table-cell-properties fo:padding="0.097cm" fo:border="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.A3" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="middle" fo:padding="0.097cm" fo:border="none" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.I4" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="middle" fo:padding="0.097cm" fo:border="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.A5" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="middle" fo:padding="0.097cm" fo:border-left="none" fo:border-right="none" fo:border-top="0.5pt solid #000000" fo:border-bottom="none" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.6" style:family="table-row">
          <style:table-row-properties style:min-row-height="0.663cm"/>
        </style:style>
        <style:style style:name="Tabela1.A6" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="middle" fo:padding="0.097cm" fo:border-left="none" fo:border-right="none" fo:border-top="none" fo:border-bottom="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.A10" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="middle" fo:padding="0.097cm" fo:border-left="none" fo:border-right="none" fo:border-top="0.5pt solid #000000" fo:border-bottom="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="Tabela1.K11" style:family="table-cell">
          <style:table-cell-properties style:vertical-align="top" fo:padding="0.097cm" fo:border-left="none" fo:border-right="none" fo:border-top="0.5pt solid #000000" fo:border-bottom="0.5pt solid #000000" style:writing-mode="page"/>
        </style:style>
        <style:style style:name="P1" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%"/>
          <style:text-properties style:font-name="Lato" fo:font-size="14pt" fo:font-style="italic" fo:font-weight="bold" style:font-size-asian="14pt" style:font-style-asian="italic" style:font-weight-asian="bold" style:font-size-complex="14pt" style:font-style-complex="italic" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P2" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="12pt" fo:font-style="italic" fo:font-weight="bold" style:font-size-asian="12pt" style:font-style-asian="italic" style:font-weight-asian="bold" style:font-size-complex="12pt" style:font-style-complex="italic" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P3" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="left"/>
          <style:text-properties style:font-name="Lato" fo:font-size="12pt" fo:font-style="normal" fo:font-weight="normal" style:font-size-asian="12pt" style:font-size-complex="12pt"/>
        </style:style>
        <style:style style:name="P4" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="left"/>
          <style:text-properties style:font-name="Lato" fo:font-size="10pt" fo:font-style="normal" fo:font-weight="normal" style:font-size-asian="10pt" style:font-size-complex="10pt"/>
        </style:style>
        <style:style style:name="P5" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P6" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-style="italic" fo:font-weight="normal" style:font-size-asian="8pt" style:font-style-asian="italic" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P7" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="left"/>
          <style:text-properties style:font-name="Lato" fo:font-size="12pt" fo:font-style="normal" fo:font-weight="bold" style:font-size-asian="12pt" style:font-weight-asian="bold" style:font-size-complex="12pt" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P8" style:family="paragraph" style:parent-style-name="Standard">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%"/>
          <style:text-properties style:font-name="Lato" fo:font-size="10pt" style:font-size-asian="10pt" style:font-size-complex="10pt"/>
        </style:style>
        <style:style style:name="P9" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="right"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-style="italic" fo:font-weight="bold" style:font-size-asian="8pt" style:font-style-asian="italic" style:font-weight-asian="bold" style:font-size-complex="8pt" style:font-style-complex="italic" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P10" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="left"/>
          <style:text-properties style:font-name="Lato" fo:font-size="7pt" fo:font-style="italic" fo:font-weight="bold" style:font-size-asian="7pt" style:font-style-asian="italic" style:font-weight-asian="bold" style:font-size-complex="7pt" style:font-style-complex="italic" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P11" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P12" style:family="paragraph" style:parent-style-name="Standard">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P13" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P14" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P15" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-weight="bold" style:font-size-asian="8pt" style:font-weight-asian="bold" style:font-size-complex="8pt" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P16" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-weight="bold" style:font-size-asian="8pt" style:font-weight-asian="bold" style:font-size-complex="8pt" style:font-weight-complex="bold"/>
        </style:style>
        <style:style style:name="P17" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P18" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P19" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P20" style:family="paragraph" style:parent-style-name="Standard">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P21" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-style="normal" fo:font-weight="normal" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P22" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-weight="normal" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P23" style:family="paragraph" style:parent-style-name="Table_20_Contents">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-weight="normal" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P24" style:family="paragraph" style:parent-style-name="Text_20_body">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="center"/>
          <style:text-properties style:font-name="Lato" fo:font-size="8pt" fo:font-weight="normal" style:font-size-asian="8pt" style:font-size-complex="8pt"/>
        </style:style>
        <style:style style:name="P25" style:family="paragraph" style:parent-style-name="Horizontal_20_Line">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%"/>
          <style:text-properties style:font-name="Lato" fo:font-size="7pt" fo:font-style="italic" style:font-size-asian="7pt" style:font-style-asian="italic" style:font-size-complex="7pt" style:font-style-complex="italic"/>
        </style:style>
        <style:style style:name="P26" style:family="paragraph" style:parent-style-name="Standard">
          <style:paragraph-properties fo:margin-top="0cm" fo:margin-bottom="0cm" fo:line-height="100%" fo:text-align="right"/>
          <style:text-properties style:font-name="Lato" fo:font-size="6pt" fo:font-style="italic" fo:font-weight="normal" style:font-size-asian="6pt" style:font-style-asian="italic" style:font-size-complex="6pt" style:font-style-complex="italic"/>
        </style:style>
        <style:style style:name="T1" style:family="text"/>
        <style:style style:name="T2" style:family="text"><style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/></style:style>
        <style:style style:name="T4" style:family="text"><style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/></style:style>
        <style:style style:name="T5" style:family="text"><style:text-properties fo:font-size="6pt" fo:font-style="italic" style:font-size-asian="6pt" style:font-style-asian="italic" style:font-size-complex="6pt" style:font-style-complex="italic"/></style:style>
        <style:style style:name="T6" style:family="text"><style:text-properties fo:font-size="6pt" fo:font-style="italic" style:font-size-asian="6pt" style:font-style-asian="italic" style:font-size-complex="6pt" style:font-style-complex="italic"/></style:style>
        <style:style style:name="T7" style:family="text"><style:text-properties fo:font-size="6pt" fo:font-style="italic" style:font-size-asian="6pt" style:font-style-asian="italic" style:font-size-complex="6pt" style:font-style-complex="italic"/></style:style>
        <style:style style:name="T8" style:family="text"><style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/></style:style>
        <style:style style:name="T11" style:family="text"><style:text-properties fo:font-weight="bold" style:font-weight-asian="bold" style:font-weight-complex="bold"/></style:style>
        <style:style style:name="T12" style:family="text"><style:text-properties fo:font-size="6pt" fo:font-style="italic" style:font-size-asian="6pt" style:font-style-asian="italic" style:font-size-complex="6pt" style:font-style-complex="italic"/></style:style>
        <style:style style:name="T14" style:family="text"><style:text-properties fo:font-size="7pt" fo:font-style="italic" style:font-size-asian="7pt" style:font-style-asian="italic" style:font-size-complex="7pt" style:font-style-complex="italic"/></style:style>
        <style:style style:name="T16" style:family="text"/>
        """;

    private static string Tabela(RelatorioRenovacao r)
    {
        var sb = new StringBuilder();

        sb.Append("""<table:table table:name="Tabela1" table:style-name="Tabela1">""");

        foreach (var col in new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K" })
            sb.Append($"""<table:table-column table:style-name="Tabela1.{col}"/>""");

        // ── Row 1: Nome + Vigência ────────────────────────────────────────────
        sb.Append("<table:table-row>");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A1" table:number-rows-spanned="2" table:number-columns-spanned="8" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P1">{X(r.NomeCliente)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 7);
        sb.Append("""<table:table-cell table:style-name="Tabela1.I1" table:number-columns-spanned="3" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P2">{X(FormatData(r.VigenciaInicial))} <text:span text:style-name="T1">até {X(FormatData(r.VigenciaFinal))}</text:span></text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 2);
        sb.Append("</table:table-row>");

        // ── Row 2: (nome coberto) + Ramo ─────────────────────────────────────
        sb.Append("<table:table-row>");
        sb.Append("""<table:covered-table-cell table:style-name="Tabela1.A1"/>""");
        Covered(sb, 7);
        sb.Append("""<table:table-cell table:style-name="Tabela1.I2" table:number-rows-spanned="2" table:number-columns-spanned="3" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P3">{X(r.Ramo)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 2);
        sb.Append("</table:table-row>");

        // ── Row 3: Info renovação + (ramo coberto) ────────────────────────────
        var acao = (r.Status?.Contains("Renov", StringComparison.OrdinalIgnoreCase) == true)
            ? "Renova a apólice" : "Apólice";
        var infoRenov = $"{acao} {r.Apolice} - {r.Seguradora} ({r.NegocioCorretora} - {r.VendedorPrincipal})";
        sb.Append("<table:table-row>");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-rows-spanned="2" table:number-columns-spanned="8" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P4">{X(infoRenov)}</text:p>""");
        sb.Append("""<text:p text:style-name="P5"/>""");
        sb.Append("""<text:p text:style-name="P6">□  PDF Importado  |  Assinatura: □ Digital   □ Trad   □ Outro:                                □ Enviada</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 7);
        sb.Append("""<table:covered-table-cell table:style-name="Tabela1.I2"/>""");
        Covered(sb, 2);
        sb.Append("</table:table-row>");

        // ── Row 4: (renov coberto) + Nova Cia ────────────────────────────────
        sb.Append("<table:table-row>");
        sb.Append("""<table:covered-table-cell table:style-name="Tabela1.A3"/>""");
        Covered(sb, 7);
        sb.Append("""<table:table-cell table:style-name="Tabela1.I4" table:number-columns-spanned="3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P7"> Nova Cia:</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 2);
        sb.Append("</table:table-row>");

        // ── Row 5: Veículo ────────────────────────────────────────────────────
        var partsVeiculo = new[] { Juntar(r.Fabricante, r.Modelo), r.Chassi, r.Placa, r.AnoFabricacao?.ToString(), r.AnoModelo?.ToString() }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        var veiculo = string.Join(" | ", partsVeiculo);
        sb.Append("<table:table-row>");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A5" table:number-columns-spanned="11" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P8">{X(veiculo)}</text:p>""");
        if (!string.IsNullOrWhiteSpace(r.CodigoDocumento))
            sb.Append($"""<text:p text:style-name="P9">{X(r.CodigoDocumento)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 10);
        sb.Append("</table:table-row>");

        // ── Row 6: Separador (linha com borda inferior) ───────────────────────
        sb.Append("""<table:table-row table:style-name="Tabela1.6">""");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A6" table:number-columns-spanned="11" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P10"/>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 10);
        sb.Append("</table:table-row>");

        // ── Row 7: Banco | Fones | Emails ────────────────────────────────────
        var banco = Juntar(r.Banco, r.Agencia, r.Conta);
        var fones = string.Join("  ",
            new[] { Juntar(r.Prefixo1, r.Telefone1), Juntar(r.Prefixo2, r.Telefone2), Juntar(r.Prefixo3, r.Telefone3) }
            .Where(p => !string.IsNullOrWhiteSpace(p)));
        var emails = Juntar(r.Email1, r.Email2);
        sb.Append("""<table:table-row table:style-name="Tabela1.6">""");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-columns-spanned="2" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P11">{X(banco)}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("<table:covered-table-cell/>");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-columns-spanned="7" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P11">{X(fones)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 6);
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-columns-spanned="2" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P11">{X(emails)}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("<table:covered-table-cell/>");
        sb.Append("</table:table-row>");

        // ── Row 8: Endereço ───────────────────────────────────────────────────
        var addr = Juntar(r.Endereco, r.NumeroEndereco, r.Complemento, r.Bairro, r.Cidade, r.Estado, r.Cep);
        sb.Append("<table:table-row>");
        sb.Append("""<table:table-cell table:style-name="Tabela1.A6" table:number-columns-spanned="11" office:value-type="string">""");
        sb.Append($"""<text:p text:style-name="P11">{X(addr)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 10);
        sb.Append("</table:table-row>");

        // ── Row 9: Perfil cliente ─────────────────────────────────────────────
        var nascStr = r.Nascimento.HasValue ? r.Nascimento.Value.ToString("dd/MM/yyyy") : "";
        var idadeStr = Idade(r.Nascimento);
        sb.Append("<table:table-row>");
        // Profissão
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-columns-spanned="2" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P12">Profissão</text:p>""");
        sb.Append($"""<text:p text:style-name="P13">{X(r.Profissao)}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("<table:covered-table-cell/>");
        // Nasc.
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Nasc.</text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(nascStr)}<text:span text:style-name="T12"> {X(idadeStr)}</text:span></text:p>""");
        sb.Append("</table:table-cell>");
        // Falecido
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Falecido</text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(r.Falecido ?? "Não")}</text:p>""");
        sb.Append("</table:table-cell>");
        // Sexo / Est. civil
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" table:number-columns-spanned="5" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Sexo        Est. civil</text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(r.Sexo)}        {X(r.EstadoCivil)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 4);
        // PCD
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">PCD/Isenção?</text:p>""");
        sb.Append("""<text:p text:style-name="P11">Não / </text:p>""");
        sb.Append("</table:table-cell>");
        // CPF/CNPJ
        sb.Append("""<table:table-cell table:style-name="Tabela1.A3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P15">CPF/CNPJ</text:p>""");
        sb.Append($"""<text:p text:style-name="P16">{X(r.DocumentoPrincipal)}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("</table:table-row>");

        // ── Row 10: Prêmios / Pagamento / Endossos ────────────────────────────
        var parcStr = r.NumeroParcelas > 0 ? $"{r.NumeroParcelas} x {Moeda(r.ValorParcelas)}" : "";
        sb.Append("<table:table-row>");
        // Prêmio líquido
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Prêmio líquido <text:span text:style-name="T14">(apl+endosos)</text:span></text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(Moeda(r.PremioLiquido))}</text:p>""");
        sb.Append("</table:table-cell>");
        // Prêmio total
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="2" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Prêmio total <text:span text:style-name="T14">(apl+endosos)</text:span></text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(Moeda(r.PremioTotal))}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("<table:covered-table-cell/>");
        // Forma pagto
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Forma de pagto <text:span text:style-name="T14">(apólice)</text:span></text:p>""");
        sb.Append($"""<text:p text:style-name="P11">{X(r.FormaPagamento)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 2);
        // Parcelamento
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="4" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P14">Parcelamento <text:span text:style-name="T14">(apólice)</text:span></text:p>""");
        sb.Append($"""<text:p text:style-name="P12">{X(parcStr)}</text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 3);
        // Endossos
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P17">Quantidade de endossos</text:p>""");
        sb.Append($"""<text:p text:style-name="P18"> {r.QuantidadeEndossos}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("</table:table-row>");

        // ── Row 11: Franquia / CEPs / Bônus / Sinistros / Bônus renovação ─────
        var franqStr = r.FranquiaApolice > 0 ? Moeda(r.FranquiaApolice) : "";
        sb.Append("<table:table-row>");
        // Franquia
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P19">Franquia</text:p>""");
        sb.Append($"""<text:p text:style-name="P20">{X(franqStr)}</text:p>""");
        sb.Append("</table:table-cell>");
        // CEPs
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="4" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P20">CEPs de Pernoite / Item / Circulação:</text:p>""");
        sb.Append($"""<text:p text:style-name="P21"><text:span text:style-name="T16">p.</text:span>{X(r.CepPernoite)} / <text:span text:style-name="T16">i.</text:span> / <text:span text:style-name="T16">c.</text:span></text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 3);
        // Bônus
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="2" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P19">Bônus</text:p>""");
        sb.Append($"""<text:p text:style-name="P22">{X(r.Bonus)}</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("<table:covered-table-cell/>");
        // Sinistros
        sb.Append("""<table:table-cell table:style-name="Tabela1.A10" table:number-columns-spanned="3" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P23">Sinistro: Quant / Valor</text:p>""");
        sb.Append($"""<text:p text:style-name="P24">{r.QuantidadeSinistros} / </text:p>""");
        sb.Append("</table:table-cell>");
        Covered(sb, 2);
        // Bônus da renovação
        sb.Append("""<table:table-cell table:style-name="Tabela1.K11" office:value-type="string">""");
        sb.Append("""<text:p text:style-name="P19">Bônus da renovação:</text:p>""");
        sb.Append("</table:table-cell>");
        sb.Append("</table:table-row>");

        sb.Append("</table:table>");
        return sb.ToString();
    }

    private static void Covered(StringBuilder sb, int count)
    {
        for (int i = 0; i < count; i++) sb.Append("<table:covered-table-cell/>");
    }
}
