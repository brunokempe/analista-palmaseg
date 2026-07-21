using System.Diagnostics;
using System.IO;

namespace AnalistaPalmaseg.Core.Services;

/// <summary>
/// Uses LibreOffice's bundled Python + UNO bridge to open password-protected ODS files
/// and export them as unencrypted XLSX for ExcelDataReader to consume.
/// </summary>
public class LibreOfficeDecryptorService
{
    private static readonly string LoPython =
        @"C:\Program Files\LibreOffice\program\python.exe";

    private static readonly string SOffice =
        @"C:\Program Files\LibreOffice\program\soffice.exe";

    public static bool IsEncryptedOds(string filePath)
    {
        if (!filePath.EndsWith(".ods", StringComparison.OrdinalIgnoreCase)) return false;
        try
        {
            using var zip = System.IO.Compression.ZipFile.OpenRead(filePath);
            // An encrypted ODS contains only "encrypted-package" and META-INF/manifest.xml
            return zip.Entries.Any(e => e.Name == "encrypted-package");
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> DecryptToXlsxAsync(string odsPath, string password)
    {
        if (!File.Exists(LoPython))
            throw new InvalidOperationException(
                "LibreOffice não encontrado em C:\\Program Files\\LibreOffice. Instale o LibreOffice para importar arquivos ODS protegidos por senha.");

        await EnsureUnoServerAsync();

        var tempOutput = Path.Combine(Path.GetTempPath(), $"palmaseg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempOutput);

        var scriptPath = Path.Combine(Path.GetTempPath(), $"palmaseg_convert_{Guid.NewGuid():N}.py");

        // LibreOffice UNO Python script: open with password, save as xlsx
        var escapedOds = odsPath.Replace("\\", "\\\\");
        var escapedOut = tempOutput.Replace("\\", "\\\\");
        var escapedPw = password.Replace("\\", "\\\\").Replace("'", "\\'");

        var script = $@"
import sys, os, time
sys.path.insert(0, r'C:\Program Files\LibreOffice\program')

import uno
from com.sun.star.beans import PropertyValue

def make_prop(name, value):
    p = PropertyValue()
    p.Name = name
    p.Value = value
    return p

ctx = uno.getComponentContext()
resolver = ctx.ServiceManager.createInstanceWithContext(
    'com.sun.star.bridge.UnoUrlResolver', ctx)

# Retry connection a few times
connected = False
for _ in range(5):
    try:
        remote_ctx = resolver.resolve(
            'uno:socket,host=localhost,port=2002;urp;StarOffice.ComponentContext')
        connected = True
        break
    except:
        time.sleep(1)

if not connected:
    print('ERROR: Could not connect to LibreOffice UNO server')
    sys.exit(1)

smgr = remote_ctx.ServiceManager
desktop = smgr.createInstanceWithContext('com.sun.star.frame.Desktop', remote_ctx)

ods_url = uno.systemPathToFileUrl(r'{escapedOds}')
props = (
    make_prop('Password', '{escapedPw}'),
    make_prop('Hidden', True),
    make_prop('MacroExecutionMode', 4),
)
doc = desktop.loadComponentFromURL(ods_url, '_blank', 0, props)

if doc is None:
    print('ERROR: Could not open file (wrong password?)')
    sys.exit(2)

out_file = os.path.join(r'{escapedOut}', 'output.xlsx')
out_url = uno.systemPathToFileUrl(out_file)

filter_props = (
    make_prop('FilterName', 'Calc MS Excel 2007 XML'),
    make_prop('Overwrite', True),
)
doc.storeToURL(out_url, filter_props)
doc.close(False)
print('SUCCESS:' + out_file)
sys.exit(0)
";

        await File.WriteAllTextAsync(scriptPath, script);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = LoPython,
                Arguments = $"\"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi)!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0 || !stdout.Contains("SUCCESS:"))
            {
                var msg = stdout.Contains("ERROR:") ? stdout : stderr;
                throw new InvalidOperationException($"Falha ao descriptografar o arquivo: {msg.Trim()}");
            }

            var outputFile = Path.Combine(tempOutput, "output.xlsx");
            if (!File.Exists(outputFile))
                throw new InvalidOperationException("Arquivo convertido não encontrado.");

            return outputFile;
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private async Task EnsureUnoServerAsync()
    {
        // Check if UNO server is already running
        if (await IsPortOpenAsync(2002)) return;

        var psi = new ProcessStartInfo
        {
            FileName = SOffice,
            Arguments = "--headless --accept=socket,host=localhost,port=2002;urp;StarOffice.ServiceManager --norestore --nofirststartwizard --nologo",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Minimized
        };

        Process.Start(psi);

        // Wait for server to start (up to 10 seconds)
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(500);
            if (await IsPortOpenAsync(2002)) return;
        }

        throw new InvalidOperationException("LibreOffice demorou muito para iniciar. Tente novamente.");
    }

    private static async Task<bool> IsPortOpenAsync(int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync("127.0.0.1", port);
            var timeoutTask = Task.Delay(300);
            var completed = await Task.WhenAny(connectTask, timeoutTask);
            return completed == connectTask && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    public static void DeleteTempDirectory(string xlsxPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(xlsxPath);
            if (dir != null && dir.StartsWith(Path.GetTempPath()))
                Directory.Delete(dir, true);
        }
        catch { /* best effort */ }
    }
}
