namespace DeviceJoiner.Services;

/// <summary>
/// 判定某个目录的 ACE 是否会让"非特权用户"获得写权限。
///
/// 背景：加域流程会创建一个开机自启、以 SYSTEM 身份运行的计划任务。
/// 如果 exe 所在目录对普通用户可写，任何本地标准用户都能替换 exe，
/// 下次开机即以 SYSTEM 执行 —— 这是一条本地提权链。
///
/// 这里只做纯判定（SID 字符串 + 布尔量），不做任何 ACL IO，
/// 便于在 CI 上脱离 Windows ACL 环境直接测试。
/// </summary>
public static class TaskPathPolicy
{
    // 非特权主体：这些 SID 覆盖"任何本地用户"或"任何已认证用户"。
    private const string Everyone = "S-1-1-0";
    private const string AuthenticatedUsers = "S-1-5-11";
    private const string Interactive = "S-1-5-4";
    private const string Anonymous = "S-1-5-7";
    private const string BuiltinUsers = "S-1-5-32-545";

    /// <summary>该 SID 是否代表非特权主体（可能是任意一个本地用户）。</summary>
    public static bool IsNonPrivilegedPrincipal(string? sid)
    {
        if (string.IsNullOrEmpty(sid))
            return true; // 解析不出 SID 时按最坏情况处理

        return sid == Everyone
            || sid == AuthenticatedUsers
            || sid == Interactive
            || sid == Anonymous
            || sid == BuiltinUsers;
    }

    /// <summary>
    /// 一条 ACE 是否构成"非特权用户可改写目录内容"的风险。
    /// 只有 Allow 规则才放权；Deny 规则反而是安全的。
    /// </summary>
    public static bool IsUnsafeAce(string? sid, bool isAllowRule, bool grantsWrite)
    {
        return isAllowRule && grantsWrite && IsNonPrivilegedPrincipal(sid);
    }
}
