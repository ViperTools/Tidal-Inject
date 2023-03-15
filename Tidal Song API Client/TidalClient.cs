using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal class TidalClient
{
    public Process Process;
    public DevToolsProtocol DevToolsProtocol;

    private static string? findAppDataTidal()
    {
        string tidalAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TIDAL");

        if (!Directory.Exists(tidalAppData))
        {
            return null;
        }

        string? appDirectory = Directory.GetDirectories(tidalAppData).FirstOrDefault(e => e.Contains("app"));

        return appDirectory != null ? Path.Join(appDirectory, "TIDAL.exe") : null;
    }

    private static string? findMicrosoftStoreTidal()
    {
        RegistryKey? key = Registry.ClassesRoot.OpenSubKey("Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppModel\\PackageRepository\\Extensions\\windows.protocol\\tidal");
        string? subkey = key?.GetSubKeyNames().FirstOrDefault();
        string? name = subkey != null ? key?.OpenSubKey(subkey)?.GetValueNames().FirstOrDefault() : null;

        if (name != null)
        {
            return Path.Join("C:\\Program Files\\WindowsApps", name, "app\\TIDAL.exe");
        }

        return null;
    }

    private static string? findTidal()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return findAppDataTidal() ?? findMicrosoftStoreTidal();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Applications/TIDAL.app/Contents/MacOS/TIDAL";
        }

        return null;
    }

    private static void closeTidal()
    {
        Process[] processes = Process.GetProcessesByName("TIDAL");

        foreach (Process p in processes)
        {
            p.Kill();
        }
    }

    public TidalClient()
    {
        closeTidal();
        string? path = findTidal();

        if (path == null)
        {
            throw new Exception("Could not find TIDAL");
        }

        Process = new Process
        {
            StartInfo =
            {
                FileName = path,
                UseShellExecute = false,
                Arguments = "--remote-debugging-port",
                EnvironmentVariables =
                {
                    { "ELECTRON_NO_ATTACH_CONSOLE", "true" }
                },
                RedirectStandardError = true,
            }
        };

        Process.Start();
        Process.StandardError.ReadLine(); // Skip empty line

        int port;
        
        if (!int.TryParse(Process.StandardError.ReadLine()?.Substring(37, 5), out port))
        {
            throw new Exception("Could not get TIDAL remote debugging port");
        }

        DevToolsProtocol = new(port);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        while (DevToolsProtocol.Target == null)
        {
            DevToolsProtocol.Target = DevToolsProtocol.GetTargets()?.FirstOrDefault(e => e.Title == "Home – TIDAL" || e.Title == "TIDAL");

            if (sw.Elapsed.TotalSeconds >= 5)
            {
                break;
            }
        }

        sw.Stop();

        if (DevToolsProtocol.Target == null)
        {
            throw new Exception("Could not find TIDAL remote debugging target");
        }

        Console.WriteLine($"Started remote debugger on port {port}");
    }
}
