using System.Runtime.InteropServices;

namespace DeviceJoiner.Services;

/// <summary>
/// 判断一次域加入失败是否值得重试。
///
/// 原则：只有"重试可能改变结果"的故障才重试。
/// WMI 的返回码（1326 口令错误、1355 域不存在、5 权限不足、64 计算机名冲突…）都是
/// 确定性结果，重试必然重复失败；而这些失败的后果是 AD 上多一次失败登录，
/// 累积起来可能触发域账户锁定策略。因此返回码永不重试，只对 RPC 瞬时故障重试。
/// </summary>
public static class JoinErrorPolicy
{
    // 0x800706BA RPC_S_SERVER_UNAVAILABLE  —— RPC 服务器不可用（服务重启、网络抖动）
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    // 0x800706BE RPC_S_CALL_FAILED         —— RPC 调用失败
    private const int RpcCallFailed = unchecked((int)0x800706BE);
    // 0x80010108 RPC_E_DISCONNECTED        —— 与对象的连接已断开
    private const int RpcDisconnected = unchecked((int)0x80010108);

    public static bool IsTransient(Exception? exception)
    {
        if (exception is not COMException com)
            return false;

        return com.HResult == RpcServerUnavailable
            || com.HResult == RpcCallFailed
            || com.HResult == RpcDisconnected;
    }
}
