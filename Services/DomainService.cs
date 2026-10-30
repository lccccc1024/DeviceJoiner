using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace DeviceJoiner.Services;

public class DomainService
{
    private const string CredFileName = ".djcred";
    private readonly string _credFilePath;
    private static readonly byte[] CredentialEntropy = Encoding.UTF8.GetBytes("DeviceJoiner credential v1");

    public DomainService()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath)
            ?? AppDomain.CurrentDomain.BaseDirectory;
        _credFilePath = Path.Combine(exeDir, CredFileName);
    }

    public void SaveCredentials(string domain, string username, string password)
    {
        var plainText = JsonSerializer.Serialize(new CredentialData(domain, username, password));
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plainText),
            CredentialEntropy,
            DataProtectionScope.LocalMachine);

        // 先写临时文件、收紧 ACL，再原子改名。
        // 直接写目标文件会有一个"已落盘但仍沿用目录继承 ACL"的窗口。
        var tempPath = _credFilePath + ".tmp";
        try
        {
            File.WriteAllBytes(tempPath, encrypted);
            RestrictCredentialFile(tempPath);
            File.Move(tempPath, _credFilePath, true);
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
            throw;
        }
    }

    /// <summary>是否存在上次留下的凭据文件（用于清理没有对应计划任务的残留）。</summary>
    public bool HasStoredCredentials => File.Exists(_credFilePath);

    public (string Domain, string Username, string Password) LoadCredentials()
    {
        if (!File.Exists(_credFilePath))
            throw new FileNotFoundException("凭证文件不存在");

        var encrypted = File.ReadAllBytes(_credFilePath);
        var plainBytes = ProtectedData.Unprotect(encrypted, CredentialEntropy, DataProtectionScope.LocalMachine);
        CredentialData? data;
        try
        {
            data = JsonSerializer.Deserialize<CredentialData>(plainBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBytes);
        }

        if (data is null || string.IsNullOrWhiteSpace(data.Domain) ||
            string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrEmpty(data.Password))
            throw new InvalidOperationException("凭证格式无效");

        return (data.Domain, data.Username, data.Password);
    }

    public void JoinDomain(string domain, string username, string password, int maxRetries = 3, int retryDelayMs = 3000)
    {
        if (maxRetries < 1)
            maxRetries = 1;

        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_ComputerSystem");
        using var collection = searcher.Get();

        foreach (ManagementBaseObject obj in collection)
        {
            using var computer = (ManagementObject)obj;

            var domainUser = username.Contains('\\') || username.Contains('@')
                ? username
                : $"{domain}\\{username}";

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                object? rawResult;
                try
                {
                    rawResult = computer.InvokeMethod("JoinDomainOrWorkgroup", new object[]
                    {
                        domain,
                        password,
                        domainUser,
                        string.Empty,
                        (uint)3 // JOIN_DOMAIN | ACCT_CREATE
                    });
                }
                catch (COMException ex) when (attempt < maxRetries && JoinErrorPolicy.IsTransient(ex))
                {
                    // RPC 瞬时故障：退避后重试。最后一次不吞异常，直接暴露给调用方。
                    System.Threading.Thread.Sleep(retryDelayMs * attempt);
                    continue;
                }

                if (rawResult == null)
                    throw new InvalidOperationException("WMI 调用 JoinDomainOrWorkgroup 返回 null，WMI 服务可能不可用");

                var result = Convert.ToInt32(rawResult);
                if (result == 0)
                    return;

                // 返回码是确定性结果（口令错误、域不存在、权限不足、计算机名冲突等），立即失败。
                // 对这些错误重试不会改变结果，只会在 AD 上累积失败登录，可能触发账户锁定策略。
                throw new DomainJoinException($"域加入失败 (WMI 返回码: {result}): {GetJoinError(result)}", result);
            }
        }

        throw new InvalidOperationException("未找到 Win32_ComputerSystem 对象");
    }

    private static string GetJoinError(int code) => code switch
    {
        0 => "成功",
        5 => "访问被拒绝，请检查域账号权限",
        64 => "计算机名格式不正确或与其他计算机冲突",
        87 => "参数无效",
        1326 => "用户名或密码错误",
        1332 => "账户名与安全标识符间无映射",
        1355 => "找不到指定的域",
        2691 => "计算机已在域中",
        2224 => "计算机账号已存在",
        _ => $"未知错误 (代码: {code})"
    };

    public void CleanupCredentials()
    {
        // 连临时文件一起清：中途失败时可能只剩 .tmp。
        foreach (var path in new[] { _credFilePath, _credFilePath + ".tmp" })
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException) { /* 文件被占用时留给下次启动清理 */ }
            catch (UnauthorizedAccessException) { /* 同上 */ }
        }
    }

    public bool IsJoinedToDomain(string domain)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT PartOfDomain, Domain FROM Win32_ComputerSystem");
        using var collection = searcher.Get();
        foreach (ManagementObject computer in collection)
        {
            using (computer)
            {
                var partOfDomain = computer["PartOfDomain"] is bool value && value;
                var currentDomain = computer["Domain"]?.ToString();
                return partOfDomain && string.Equals(currentDomain, domain, StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    private static void RestrictCredentialFile(string path)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(true, false);
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }

    private sealed record CredentialData(string Domain, string Username, string Password);
}
