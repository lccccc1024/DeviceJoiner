namespace DeviceJoiner.Services;

/// <summary>
/// 域加入失败。携带 WMI 返回码，并标明该失败是否值得重试。
///
/// 返回码类失败（口令错误、域不存在、权限不足、计算机名冲突）都是确定性结果，
/// 跨开机重试也不会改变结果，只会继续在 AD 上累积失败登录，因此 IsRetryable 恒为 false。
/// 瞬时故障（RPC 不可用等）以 COMException 形式抛出，不经过本类型，外层仍会重试。
/// </summary>
public sealed class DomainJoinException : Exception
{
    public DomainJoinException(string message, int wmiReturnCode, Exception? innerException = null)
        : base(message, innerException)
    {
        WmiReturnCode = wmiReturnCode;
    }

    public int WmiReturnCode { get; }

    /// <summary>返回码类失败一律不重试。</summary>
    public bool IsRetryable => false;
}
