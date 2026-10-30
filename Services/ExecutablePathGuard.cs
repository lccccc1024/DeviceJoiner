using System.Security.AccessControl;
using System.Security.Principal;

namespace DeviceJoiner.Services;

/// <summary>
/// 校验 exe 所在目录是否"只有管理员能改写"。
///
/// 开机自启任务以 SYSTEM 身份运行 exe；若该目录对普通用户可写，
/// 任何本地标准用户都能替换 exe，下次开机即以 SYSTEM 执行。
/// 因此创建任务前必须先过这道检查，不通过就中止。
/// </summary>
public static class ExecutablePathGuard
{
    /// <summary>能替换 exe 的权限：改内容、删文件、改权限、换属主。</summary>
    private const FileSystemRights WriteLikeRights =
        FileSystemRights.Write
        | FileSystemRights.Delete
        | FileSystemRights.DeleteSubdirectoriesAndFiles
        | FileSystemRights.ChangePermissions
        | FileSystemRights.TakeOwnership;

    public static void EnsureDirectoryIsProtected(string executablePath)
    {
        var directory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new InvalidOperationException(
                $"无法确定程序所在目录：{executablePath}。请把 DeviceJoiner.exe 放到本地受保护目录后重试。");
        }

        DirectorySecurity security;
        try
        {
            security = new DirectoryInfo(directory).GetAccessControl(AccessControlSections.Access);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
                                       or IOException
                                       or PlatformNotSupportedException
                                       or IdentityNotMappedException)
        {
            throw new InvalidOperationException(
                $"无法读取程序所在目录的权限设置（{directory}）：{ex.Message}。" +
                "为避免把开机自启任务指向可能被替换的程序，已中止。" +
                "请把 DeviceJoiner.exe 放到仅管理员可写的目录（例如 C:\\Program Files\\DeviceJoiner\\）后重试。",
                ex);
        }

        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            var grantsWrite = (rule.FileSystemRights & WriteLikeRights) != 0;
            var isAllow = rule.AccessControlType == AccessControlType.Allow;

            if (!TaskPathPolicy.IsUnsafeAce(rule.IdentityReference.Value, isAllow, grantsWrite))
                continue;

            throw new InvalidOperationException(
                $"程序所在目录允许非管理员用户写入：{directory}（{rule.IdentityReference}）。" +
                "本工具的开机自启任务会以 SYSTEM 身份运行这里的 exe，" +
                "普通用户替换该文件即可获得系统权限，因此拒绝创建任务。" +
                "请把 DeviceJoiner.exe 移动到仅管理员可写的目录（例如 C:\\Program Files\\DeviceJoiner\\）后重试。");
        }
    }
}
