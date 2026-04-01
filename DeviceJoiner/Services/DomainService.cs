using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace DeviceJoiner.Services;

public class DomainService
{
    private const string CredFileName = ".djcred";
    private readonly string _credFilePath;

    public DomainService()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath)
            ?? AppDomain.CurrentDomain.BaseDirectory;
        _credFilePath = Path.Combine(exeDir, CredFileName);
    }

    public void SaveCredentials(string domain, string username, string password)
    {
        var plainText = $"{domain}|{username}|{password}";
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(plainText),
            null,
            DataProtectionScope.LocalMachine);
        File.WriteAllBytes(_credFilePath, encrypted);
    }

    public (string Domain, string Username, string Password) LoadCredentials()
    {
        if (!File.Exists(_credFilePath))
            throw new FileNotFoundException("凭证文件不存在");

        var encrypted = File.ReadAllBytes(_credFilePath);
        var plainBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);
        var plainText = Encoding.UTF8.GetString(plainBytes);
        var parts = plainText.Split('|', 3);

        if (parts.Length != 3)
            throw new InvalidOperationException("凭证格式无效");

        return (parts[0], parts[1], parts[2]);
    }

    public void JoinDomain(string domain, string username, string password)
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_ComputerSystem");
        using var collection = searcher.Get();

        foreach (ManagementBaseObject obj in collection)
        {
            using var computer = (ManagementObject)obj;

            var domainUser = username.Contains('\\') || username.Contains('@')
                ? username
                : $"{domain}\\{username}";

            var result = Convert.ToInt32(computer.InvokeMethod("JoinDomainOrWorkgroup", new object[]
            {
                domain,            // Name - domain to join
                password,          // Password
                domainUser,        // UserName - domain\user format
                string.Empty,      // AccountOU
                (uint)1            // FJoinOptions - 1 = account creation
            }));

            if (result != 0)
                throw new InvalidOperationException($"域加入失败 (WMI 返回码: {result}): {GetJoinError(result)}");

            return;
        }

        throw new InvalidOperationException("未找到 Win32_ComputerSystem 对象");
    }

    private static string GetJoinError(int code) => code switch
    {
        0 => "成功",
        5 => "访问被拒绝，请检查域账号权限",
        87 => "参数无效",
        1326 => "用户名或密码错误",
        1355 => "找不到指定的域",
        2691 => "计算机已在域中",
        2224 => "计算机账号已存在",
        _ => $"未知错误 (代码: {code})"
    };

    public void CleanupCredentials()
    {
        if (File.Exists(_credFilePath))
            File.Delete(_credFilePath);
    }
}
