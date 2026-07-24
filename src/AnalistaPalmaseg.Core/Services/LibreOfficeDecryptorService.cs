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

        // Copy ODS to a temp path with ASCII-only name to avoid encoding issues
        // with accented characters in sys.argv on LibreOffice's bundled Python.
        var tempInput = Path.Combine(Path.GetTempPath(), $"palmaseg_src_{Guid.NewGuid():N}.ods");
        File.Copy(odsPath, tempInput, overwrite: true);

        var tempOutput = Path.Combine(Path.GetTempPath(), $"palmaseg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempOutput);
        var outputFile = Path.Combine(tempOutput, "output.xlsx");

        // Write a password file so we don't expose it on the command line
        var pwFile = Path.Combine(Path.GetTempPath(), $"palmaseg_pw_{Guid.NewGuid():N}.txt");
        var scriptPath = Path.Combine(Path.GetTempPath(), $"palmaseg_convert_{Guid.NewGuid():N}.py");

        // Paths and password are passed via argv / file — no string interpolation in the script body
        var script = @"
import sys, os, time
sys.path.insert(0, r'C:\Program Files\LibreOffice\program')

import uno
from com.sun.star.beans import PropertyValue

# Args: script ods_path out_file pw_file
ods_path = sys.argv[1]
out_file  = sys.argv[2]
pw_file   = sys.argv[3]

with open(pw_file, 'r', encoding='utf-8-sig') as f:
    password = f.read().strip()

def make_prop(name, value):
    p = PropertyValue()
    p.Name = name
    p.Value = value
    return p

ctx = uno.getComponentContext()
resolver = ctx.ServiceManager.createInstanceWithContext(
    'com.sun.star.bridge.UnoUrlResolver', ctx)

connected = False
for _ in range(10):
    try:
        remote_ctx = resolver.resolve(
            'uno:socket,host=localhost,port=2002;urp;StarOffice.ComponentContext')
        connected = True
        break
    except:
        time.sleep(0.5)

if not connected:
    print('ERROR: Could not connect to LibreOffice UNO server')
    sys.exit(1)

smgr = remote_ctx.ServiceManager
desktop = smgr.createInstanceWithContext('com.sun.star.frame.Desktop', remote_ctx)

ods_url = uno.systemPathToFileUrl(ods_path)
print('DEBUG: ods_url=' + ods_url)
props = (
    make_prop('Password', password),
    make_prop('Hidden', True),
    make_prop('MacroExecutionMode', 4),
)
try:
    doc = desktop.loadComponentFromURL(ods_url, '_blank', 0, props)
except Exception as ex:
    print('ERROR: Exception in loadComponentFromURL: ' + str(ex))
    sys.exit(3)

if doc is None:
    print('ERROR: doc is None — file not found or password rejected')
    sys.exit(2)

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

        // Use UTF-8 without BOM — BOM would prepend ﻿ to the password Python reads
        var utf8NoBom = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        await File.WriteAllTextAsync(scriptPath, script, utf8NoBom);
        await File.WriteAllTextAsync(pwFile, password, utf8NoBom);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = LoPython,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };
            // Use ArgumentList for correct quoting of paths with spaces/special chars
            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add(tempInput);   // ASCII-only temp copy, no encoding issues
            psi.ArgumentList.Add(outputFile);
            psi.ArgumentList.Add(pwFile);

            using var proc = Process.Start(psi)!;
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (proc.ExitCode != 0 || !stdout.Contains("SUCCESS:"))
            {
                var detail = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
                throw new InvalidOperationException($"Falha ao descriptografar o arquivo:\n{detail.Trim()}");
            }

            if (!File.Exists(outputFile))
                throw new InvalidOperationException("Arquivo convertido não encontrado após a exportação.");

            return outputFile;
        }
        finally
        {
            File.Delete(scriptPath);
            File.Delete(pwFile);
            File.Delete(tempInput);
            StopUnoServer();
        }
    }

    private Process? _unoServer;

    private async Task EnsureUnoServerAsync()
    {
        if (await IsPortOpenAsync(2002)) return;

        var psi = new ProcessStartInfo
        {
            FileName = SOffice,
            Arguments = "--headless --accept=socket,host=localhost,port=2002;urp;StarOffice.ServiceManager --norestore --nofirststartwizard --nologo",
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Minimized
        };

        _unoServer = Process.Start(psi);

        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(500);
            if (await IsPortOpenAsync(2002)) return;
        }

        throw new InvalidOperationException("LibreOffice demorou muito para iniciar. Tente novamente.");
    }

    private void StopUnoServer()
    {
        // Mata o processo rastreado
        try
        {
            if (_unoServer is { HasExited: false })
            {
                _unoServer.Kill(entireProcessTree: true);
                _unoServer.WaitForExit(3000);
            }
        }
        catch { }
        finally
        {
            _unoServer?.Dispose();
            _unoServer = null;
        }

        // soffice.exe pode spawnar processos filhos (soffice.bin, etc.) — mata todos
        foreach (var name in new[] { "soffice", "soffice.bin" })
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try { p.Kill(entireProcessTree: true); p.WaitForExit(2000); } catch { }
                finally { p.Dispose(); }
            }
        }

        // Remove o lock de perfil órfão que impede abrir o LibreOffice normalmente
        var lockFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LibreOffice", "4", ".lock");
        try { if (File.Exists(lockFile)) File.Delete(lockFile); } catch { }
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
