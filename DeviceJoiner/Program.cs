using System.Diagnostics;
using System.Security.Principal;
using DeviceJoiner.Config;
using DeviceJoiner.Services;

namespace DeviceJoiner;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (!IsAdministrator())
        {
            ElevateToAdmin(args);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (args.Length > 0 && args[0] == "--autojoin")
        {
            RunAutoJoin();
        }
        else
        {
            Application.Run(new MainForm());
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void ElevateToAdmin(string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            Arguments = string.Join(" ", args),
            Verb = "runas",
            UseShellExecute = true
        };

        try
        {
            Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // User cancelled UAC prompt
        }
    }

    private static void RunAutoJoin()
    {
        var configManager = new ConfigManager();
        var config = configManager.Load();
        var logger = new Logger(config.LogPath);
        var domainService = new DomainService();
        var taskService = new ScheduledTaskService();

        try
        {
            logger.Info("自动加入模式启动");

            var (domain, username, password) = domainService.LoadCredentials();
            logger.Info($"读取凭证成功，域: {domain}");

            var retryCount = taskService.GetRetryCount();
            logger.Info($"当前重试次数: {retryCount}/{config.MaxRetryCount}");

            if (retryCount >= config.MaxRetryCount)
            {
                logger.Error($"已达到最大重试次数 ({config.MaxRetryCount})，放弃域加入");
                taskService.DeleteTask();
                taskService.ClearRetryCount();
                domainService.CleanupCredentials();
                return;
            }

            logger.Info("开始加入域...");
            domainService.JoinDomain(domain, username, password);
            logger.Info("域加入成功！");

            taskService.DeleteTask();
            taskService.ClearRetryCount();
            domainService.CleanupCredentials();
            logger.Info("清理完成");
        }
        catch (Exception ex)
        {
            logger.Error($"自动加入失败: {ex.Message}");
            taskService.IncrementRetryCount();
            logger.Info("已更新重试计数，下次重启将重试");
        }
    }
}
