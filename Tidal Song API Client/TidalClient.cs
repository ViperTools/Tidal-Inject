using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal class TidalClient
{
    Process process;

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
        // Add Microsoft Store app, Add MacOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return findAppDataTidal() ?? findMicrosoftStoreTidal();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "/Applications/TIDAL";
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

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    public TidalClient()
    {
        closeTidal();
        string? path = findTidal();

        if (path == null)
        {
            throw new Exception("Could not find TIDAL");
        }

        process = new Process
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

        process.ErrorDataReceived += (sender, args) => Debug.WriteLine("OUTPUT: " + args.Data);

        process.Start();

        process.BeginErrorReadLine();

        process.WaitForExit();
    }
}
