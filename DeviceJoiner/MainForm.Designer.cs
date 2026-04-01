namespace DeviceJoiner;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.Text = "设备主机名修改 & 域加入工具";
        this.Size = new System.Drawing.Size(580, 520);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        var y = 15;
        var labelX = 15;
        var inputX = 140;
        var inputWidth = 400;

        lblSn = new Label { Text = "BIOS SN:", Location = new Point(labelX, y), AutoSize = true };
        txtSn = new TextBox { Location = new Point(inputX, y - 3), Width = inputWidth, ReadOnly = true, BackColor = SystemColors.Control };
        y += 35;

        lblCurrentHost = new Label { Text = "当前主机名:", Location = new Point(labelX, y), AutoSize = true };
        txtCurrentHost = new TextBox { Location = new Point(inputX, y - 3), Width = inputWidth, ReadOnly = true, BackColor = SystemColors.Control };
        y += 45;

        var grpConfig = new GroupBox { Text = "配置", Location = new Point(10, y), Size = new Size(545, 180) };
        var gy = 25;

        lblTemplate = new Label { Text = "主机名模板:", Location = new Point(10, gy), AutoSize = true };
        txtTemplate = new TextBox { Location = new Point(100, gy - 3), Width = 200, Text = "{prefix}-{sn}" };
        gy += 30;

        lblPrefix = new Label { Text = "前缀:", Location = new Point(10, gy), AutoSize = true };
        txtPrefix = new TextBox { Location = new Point(100, gy - 3), Width = 100, Text = "WS" };
        gy += 30;

        lblDomain = new Label { Text = "域名:", Location = new Point(10, gy), AutoSize = true };
        txtDomain = new TextBox { Location = new Point(100, gy - 3), Width = 300 };
        gy += 30;

        lblUser = new Label { Text = "域账号:", Location = new Point(10, gy), AutoSize = true };
        txtUser = new TextBox { Location = new Point(100, gy - 3), Width = 200 };
        gy += 30;

        lblPass = new Label { Text = "域密码:", Location = new Point(10, gy), AutoSize = true };
        txtPass = new TextBox { Location = new Point(100, gy - 3), Width = 200, PasswordChar = '*' };

        grpConfig.Controls.AddRange(new Control[] {
            lblTemplate, txtTemplate, lblPrefix, txtPrefix,
            lblDomain, txtDomain, lblUser, txtUser, lblPass, txtPass
        });
        y += 190;

        lblPreview = new Label { Text = "预览主机名:", Location = new Point(labelX, y), AutoSize = true };
        txtPreview = new TextBox { Location = new Point(inputX, y - 3), Width = inputWidth, ReadOnly = true, BackColor = SystemColors.Info, Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Bold) };
        y += 40;

        btnExecute = new Button { Text = "执行", Location = new Point(140, y), Width = 100, Height = 35 };
        btnSave = new Button { Text = "保存配置", Location = new Point(260, y), Width = 100, Height = 35 };
        btnCancel = new Button { Text = "取消", Location = new Point(380, y), Width = 100, Height = 35 };
        y += 50;

        var grpLog = new GroupBox { Text = "日志", Location = new Point(10, y), Size = new Size(545, 150) };
        txtLog = new RichTextBox { Location = new Point(10, 20), Size = new Size(525, 120), ReadOnly = true, BackColor = SystemColors.Window, Font = new Font("Consolas", 8.5f) };
        grpLog.Controls.Add(txtLog);

        this.Controls.AddRange(new Control[] {
            lblSn, txtSn, lblCurrentHost, txtCurrentHost,
            grpConfig, lblPreview, txtPreview,
            btnExecute, btnSave, btnCancel, grpLog
        });

        txtTemplate.TextChanged += OnConfigChanged;
        txtPrefix.TextChanged += OnConfigChanged;
        btnExecute.Click += BtnExecute_Click;
        btnSave.Click += BtnSave_Click;
        btnCancel.Click += (s, e) => this.Close();
    }

    private Label lblSn;
    private TextBox txtSn;
    private Label lblCurrentHost;
    private TextBox txtCurrentHost;
    private Label lblTemplate;
    private TextBox txtTemplate;
    private Label lblPrefix;
    private TextBox txtPrefix;
    private Label lblDomain;
    private TextBox txtDomain;
    private Label lblUser;
    private TextBox txtUser;
    private Label lblPass;
    private TextBox txtPass;
    private Label lblPreview;
    private TextBox txtPreview;
    private Button btnExecute;
    private Button btnSave;
    private Button btnCancel;
    private RichTextBox txtLog;
}
