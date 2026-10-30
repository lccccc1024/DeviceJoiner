using System;
using System.Runtime.InteropServices;
using DeviceJoiner.Services;

namespace DeviceJoiner.Tests;

/// <summary>
/// 零依赖测试运行器：退出码非 0 即为失败，供 CI 直接判断。
/// </summary>
internal static class Program
{
    private static int _failures;

    private static void Check(bool condition, string name)
    {
        if (condition)
        {
            Console.WriteLine($"[PASS] {name}");
            return;
        }

        Console.WriteLine($"[FAIL] {name}");
        _failures++;
    }

    private static COMException Com(int hresult) => new("test", hresult);

    private static int Main()
    {
        // RPC 瞬时故障 → 可重试
        Check(JoinErrorPolicy.IsTransient(Com(unchecked((int)0x800706BA))),
            "RPC_S_SERVER_UNAVAILABLE 可重试");
        Check(JoinErrorPolicy.IsTransient(Com(unchecked((int)0x800706BE))),
            "RPC_S_CALL_FAILED 可重试");
        Check(JoinErrorPolicy.IsTransient(Com(unchecked((int)0x80010108))),
            "RPC_E_DISCONNECTED 可重试");

        // 非瞬时故障 → 不重试（避免累积失败登录）
        Check(!JoinErrorPolicy.IsTransient(Com(unchecked((int)0x80070005))),
            "E_ACCESSDENIED 不重试");
        Check(!JoinErrorPolicy.IsTransient(Com(unchecked((int)0x80004005))),
            "E_FAIL 不重试");
        Check(!JoinErrorPolicy.IsTransient(Com(unchecked((int)0x80070035))),
            "网络路径未找到 不重试");
        Check(!JoinErrorPolicy.IsTransient(Com(0)),
            "HResult 为 0 不重试");

        // 非 COM 异常与 null
        Check(!JoinErrorPolicy.IsTransient(new InvalidOperationException("x")),
            "非 COM 异常不重试");
        Check(!JoinErrorPolicy.IsTransient(null),
            "null 不重试");

        // 返回码类失败通过 DomainJoinException 透传，外层据此放弃跨开机重试
        var joinFailure = new DomainJoinException("域加入失败", 1326);
        Check(!joinFailure.IsRetryable, "DomainJoinException 默认不可重试");
        Check(joinFailure.WmiReturnCode == 1326, "DomainJoinException 保留 WMI 返回码");
        Check(joinFailure is Exception, "DomainJoinException 可被通用 catch 捕获");

        Console.WriteLine();
        Console.WriteLine(_failures == 0
            ? "全部通过"
            : $"{_failures} 项失败");
        return _failures == 0 ? 0 : 1;
    }
}
