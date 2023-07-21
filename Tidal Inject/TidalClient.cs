namespace TidalInject;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class TidalClient
{
    public Process Process;
    public DevToolsProtocol ClientProtocol, NodeProtocol;

    private static string? FindAppDataTidal()
    {
        string tidalAppData = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TIDAL");

        if (!Directory.Exists(tidalAppData))
        {
            return null;
        }

        string? appDirectory = Directory.GetDirectories(tidalAppData).FirstOrDefault(e => e.Contains("app"));

        return appDirectory != null ? Path.Join(appDirectory, "TIDAL.exe") : null;
    }

    private static string? FindMicrosoftStoreTidal()
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

    private static string? FindTidal()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return FindAppDataTidal() ?? FindMicrosoftStoreTidal();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Applications/TIDAL.app/Contents/MacOS/TIDAL";
        }

        return null;
    }

    private static void CloseTidal()
    {
        Process[] processes = Process.GetProcessesByName("TIDAL");

        foreach (Process p in processes)
        {
            p.Kill();
        }
    }

    private int ReadPort()
    {
        string? url = Process.StandardError.ReadLine();

        if (url != null && int.TryParse(Regex.Match(url, @":(\d+)").Groups[1].Value, out int port))
        {
            return port;
        }

        return 0;
    }

    private static bool InitProtocol(DevToolsProtocol protocol)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        while (protocol.Target == null)
        {
            protocol.Target = protocol.GetTargets()?.FirstOrDefault(e => !e.Title.Contains("Service"));

            if (sw.Elapsed.TotalSeconds >= 5)
            {
                return false;
            }
        }

        sw.Stop();

        return true;
    }

    public TidalClient()
    {
        CloseTidal();
        string? path = FindTidal();

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
                Arguments = "--remote-debugging-port --inspect",
                EnvironmentVariables =
                {
                    { "ELECTRON_NO_ATTACH_CONSOLE", "true" }
                },
                RedirectStandardError = true,
            }
        };

        Process.Start();
        int nodePort = ReadPort();

        if (nodePort == 0)
        {
            throw new Exception("Could not get Node debugging port");
        }

        Process.StandardError.ReadLine();
        Process.StandardError.ReadLine();

        int clientPort = ReadPort();

        if (clientPort == 0)
        {
            throw new Exception("Could not get TIDAL remote debugging port");
        }

        NodeProtocol = new(nodePort);

        if (!InitProtocol(NodeProtocol))
        {
            throw new Exception("Could not find node remote debugging target");
        }

        ClientProtocol = new(clientPort);
        
        if (!InitProtocol(ClientProtocol))
        {
            throw new Exception("Could not find TIDAL remote debugging target");
        }

        Console.WriteLine($"Started Node debugger on port {nodePort}");
        Console.WriteLine($"Started remote debugger on port {clientPort}");
    }
}
