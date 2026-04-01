# DeviceJoiner

Windows 设备加域工具。通过 GUI 读取 BIOS 序列号自动生成主机名，修改主机名并加入 Active Directory 域。

## 功能

- 读取 BIOS 序列号，按可配置模板生成主机名
- 修改主机名（WMI）
- 直接加入 Active Directory 域（WMI）
- 操作过程实时日志显示

## 环境要求

- Windows 10/11 x64
- 管员员权限
- 可访问的域控制器

## 构建

```cmd
cd DeviceJoiner
dotnet publish DeviceJoiner.csproj -c Release
```

生成的单文件 exe 位于：

```
bin\Release\net8.0-windows\win-x64\publish\DeviceJoiner.exe
```

自包含发布，无需安装 .NET 运行时。

## 使用

1. 以管理员身份运行 `DeviceJoiner.exe`（会自动请求 UAC 提权）
2. 确认 BIOS 序列号和当前主机名
3. 配置主机名前缀、域名、域账号密码
4. 点击「执行」完成主机名修改和域加入
5. 重启使主机名生效

## 配置

配置文件 `Config/appsettings.json`：

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `HostnameTemplate` | 主机名模板，`{prefix}` 和 `{sn}` 为占位符 | `{prefix}-{sn}` |
| `Prefix` | 主机名前缀 | `WS` |
| `Domain` | 域名 | - |
| `LogPath` | 日志路径 | `logs/app.log` |

## 项目结构

```
DeviceJoiner/
├── DeviceJoiner.sln
├── DeviceJoiner.csproj
├── Program.cs                    # 入口，管理员提权
├── MainForm.cs                   # 主界面
├── MainForm.Designer.cs          # 界面布局
├── Config/
│   ├── AppConfig.cs              # 配置模型
│   ├── ConfigManager.cs          # 配置读写
│   └── appsettings.json          # 默认配置
└── Services/
    ├── BiosService.cs            # BIOS 序列号读取
    ├── HostnameService.cs        # 主机名模板解析与修改
    ├── DomainService.cs          # WMI 域加入
    └── Logger.cs                 # 日志服务
```
