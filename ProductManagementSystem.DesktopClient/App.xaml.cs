using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ProductManagementSystem.DesktopClient;

public partial class App : Application
{
    private Process? _apiProcess;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var apiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProductManagementSystem.Api.exe");
        if (File.Exists(apiPath))
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = apiPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                _apiProcess = Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao iniciar os servicos locais: " + ex.Message, "Falha na Inicializacao", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_apiProcess != null && !_apiProcess.HasExited)
            {
                _apiProcess.Kill();
                _apiProcess.Dispose();
            }
        }
        catch { }
        base.OnExit(e);
    }
}
