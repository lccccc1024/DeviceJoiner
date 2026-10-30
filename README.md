# DeviceJoiner

Windows 设备加域工具。通过 GUI 读取 BIOS 序列号自动生成主机名，修改主机名并加入 Active Directory 域。

## 功能

- 读取 BIOS 序列号，按可配置模板生成主机名
- 修改主机名（WMI）
- 直接加入 Active Directory 域（Windows WMI）
- 操作过程实时日志显示

## 环境要求

- Windows 10/11 x64
- 管理员权限
- 可访问的域控制器

## 构建

```cmd
dotnet publish DeviceJoiner.csproj -c Release
```

生成的单文件 exe 位于：

```
bin\Release\net8.0-windows\win-x64\publish\DeviceJoiner.exe
```

自包含发布，无需安装 .NET 运行时。

## 使用

1. 把 `DeviceJoiner.exe` 放在**仅管理员可写**的目录（推荐 `C:\Program Files\DeviceJoiner\`），再以管理员身份运行（会自动请求 UAC 提权）。若所在目录允许普通用户写入（桌面、下载目录、公共共享盘等），工具会拒绝创建开机自启任务并说明原因
2. 确认 BIOS 序列号和当前主机名
3. 配置主机名前缀、域名、域账号密码
4. 点击「执行」完成主机名修改和域加入
5. 如提示成功，按提示重启；如果**网络或域控暂时不可用**，工具会在下次开机自动重试（最多 `MaxRetryCount` 次）。口令错误、域不存在、权限不足这类确定性失败会**立即放弃并清理**，不会反复重试——避免在 AD 上累积失败登录、触发账户锁定策略

## 安全说明

- 域凭据仅为跨重启恢复流程临时保存，使用 Windows DPAPI（LocalMachine）加密，并限制文件 ACL 为 SYSTEM 和本地 Administrators。
- 创建开机自启任务前会校验 exe 所在目录的权限：只要存在 `Everyone` / `Users` / `Authenticated Users` / `INTERACTIVE` 等非特权主体的可写 ACE（含继承），就直接拒绝创建任务。原因是该任务以 SYSTEM 身份运行 exe，普通用户替换 exe 即可获得系统权限。
- 加域成功或达到最大重试次数后会删除凭据和计划任务。
- `LocalMachine` DPAPI 不能抵御本机 SYSTEM/管理员级恶意软件。批量部署场景建议改用 Offline Domain Join（`djoin.exe`），避免在客户端保存域密码。

## 配置

配置文件为发布目录下的 `config/appsettings.json`（发布时自动复制）：

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `HostnameTemplate` | 主机名模板，`{prefix}` 和 `{sn}` 为占位符 | `{prefix}-{sn}` |
| `Prefix` | 主机名前缀 | `WS` |
| `Domain` | 域名 | - |
| `LogPath` | 日志路径 | `logs/app.log` |

## 项目结构

```
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
    ├── ScheduledTaskService.cs   # 计划任务管理
    └── Logger.cs                 # 日志服务
```
