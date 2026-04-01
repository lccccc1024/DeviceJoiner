using System.Diagnostics;
using DeviceJoiner.Config;
using DeviceJoiner.Services;

namespace DeviceJoiner;

public partial class MainForm : Form
{
    private readonly AppConfig _config;
    private readonly ConfigManager _configManager;
    private readonly BiosService _biosService;
    private readonly HostnameService _hostnameService;
    private readonly DomainService _domainService;
    private readonly Logger _logger;
    private string _sn = "";

    public MainForm()
    {
        InitializeComponent();

        _configManager = new ConfigManager();
        _config = _configManager.Load();
        _biosService = new BiosService();
        _hostnameService = new HostnameService();
        _domainService = new DomainService();
        _logger = new Logger(_config.LogPath);

        _logger.OnLog += line =>
        {
            if (txtLog.InvokeRequired)
                txtLog.Invoke(() => AppendLog(line));
            else
                AppendLog(line);
        };

        LoadInitialData();
    }

    private void LoadInitialData()
    {
        try
        {
            _sn = _biosService.GetSerialNumber();
            txtSn.Text = _sn;
            _logger.Info($"读取 BIOS SN: {_sn}");
        }
        catch (Exception ex)
        {
            txtSn.Text = "读取失败";
            _logger.Error($"读取 SN 失败: {ex.Message}");
        }

        txtCurrentHost.Text = _biosService.GetCurrentHostname();
        _logger.Info($"当前主机名: {txtCurrentHost.Text}");

        txtTemplate.Text = _config.HostnameTemplate;
        txtPrefix.Text = _config.Prefix;
        txtDomain.Text = _config.Domain;
        txtUser.Text = _config.DomainUser;

        UpdatePreview();
    }

    private void OnConfigChanged(object? sender, EventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (string.IsNullOrEmpty(_sn))
        {
            txtPreview.Text = "(等待读取 SN)";
            return;
        }

        var hostname = _hostnameService.BuildHostname(txtTemplate.Text, txtPrefix.Text, _sn);
        var valid = _hostnameService.IsValidHostname(hostname);
        txtPreview.Text = hostname;
        txtPreview.BackColor = valid ? SystemColors.Info : Color.MistyRose;
    }

    private void AppendLog(string line)
    {
        txtLog.AppendText(line + Environment.NewLine);
        txtLog.ScrollToCaret();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _config.HostnameTemplate = txtTemplate.Text;
        _config.Prefix = txtPrefix.Text;
        _config.Domain = txtDomain.Text;
        _config.DomainUser = txtUser.Text;
        _configManager.Save(_config);
        _logger.Info("配置已保存");
        MessageBox.Show("配置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnExecute_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_sn))
        {
            MessageBox.Show("BIOS SN 未读取，无法执行", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var newHostname = _hostnameService.BuildHostname(txtTemplate.Text, txtPrefix.Text, _sn);

        if (!_hostnameService.IsValidHostname(newHostname))
        {
            MessageBox.Show($"主机名 '{newHostname}' 不符合 Windows 规则（15字符内，仅字母数字连字符）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtDomain.Text))
        {
            MessageBox.Show("请输入域名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text))
        {
            MessageBox.Show("请输入域账号和密码", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var hostnameChanged = !string.Equals(txtCurrentHost.Text, newHostname, StringComparison.OrdinalIgnoreCase);
        var confirmMsg = hostnameChanged
            ? $"主机名将从 '{txtCurrentHost.Text}' 改为 '{newHostname}'，并立即加入域 '{txtDomain.Text}'\n\n确认执行？执行后需要重启才能使主机名生效。"
            : $"主机名 '{newHostname}' 无需修改，将直接加入域 '{txtDomain.Text}'\n\n确认执行？";

        var confirm = MessageBox.Show(confirmMsg, "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes)
            return;

        btnExecute.Enabled = false;

        try
        {
            if (hostnameChanged)
            {
                _logger.Info($"开始修改主机名: {txtCurrentHost.Text} -> {newHostname}");
                _hostnameService.SetHostname(newHostname);
                _logger.Info("主机名修改成功");
            }
            else
            {
                _logger.Info($"主机名无需修改: {newHostname}");
            }

            _logger.Info($"开始加入域: {txtDomain.Text}");
            _domainService.JoinDomain(txtDomain.Text, txtUser.Text, txtPass.Text);
            _logger.Info("域加入成功！");

            _logger.Info("保存配置...");
            _config.HostnameTemplate = txtTemplate.Text;
            _config.Prefix = txtPrefix.Text;
            _config.Domain = txtDomain.Text;
            _config.DomainUser = txtUser.Text;
            _configManager.Save(_config);

            var result = MessageBox.Show(
                "操作完成！\n\n" + (hostnameChanged ? "主机名已修改并成功加入域，重启后生效。" : "已成功加入域。") + "\n\n是否立即重启？",
                "完成",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                Process.Start("shutdown", "/r /t 5 /c \"设备将重启以完成域加入\"");
                Application.Exit();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"执行失败: {ex.Message}");
            MessageBox.Show($"执行失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnExecute.Enabled = true;
        }
    }
}
