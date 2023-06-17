namespace Palaflake;

using System;
using System.Threading;

public class Generator
{
    private readonly DateTime _start;

    private readonly long _instanceId;
    private long _lastTimestamp;

    private long _cb; //回拨次数
    private long _seq; //序列号

    /// <summary>
    /// 构造palaflake生成器
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="startYear">计时起始年</param>
    public Generator(byte instanceId, ushort startYear)
    {
        //设置了未来时间
        if (DateTime.UtcNow.Year < startYear)
            throw new Exception($"The startYear({startYear}) cannot be set to a future time");

        //时间戳溢出
        if (DateTime.UtcNow.Year - startYear >= 34)
            throw new Exception($"The startYear({startYear}) cannot older than 34 years");

        _start = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        this._instanceId = instanceId;
    }

    private object _mutex = new();

    //ID结构参考
    //01112222 22222222 22222222 22222222
    //22222222 22223333 33334444 44444444

    /// <summary>
    /// 生成ID
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public long Next()
    {
        lock (_mutex)
        {
            var utcNow = DateTime.UtcNow;

            if (utcNow < _start) //当前时间早于起始时间
                throw new Exception($"Illegal system time({utcNow})");

            var currTimestamp = (long)(utcNow - _start).TotalMilliseconds;

            if (currTimestamp > _lastTimestamp)
                _seq = 0;
            else if (currTimestamp == _lastTimestamp)
            {
                _seq++;
                if (_seq > 4095) //一毫秒内的请求超过4096次
                {
                    Thread.Sleep(1); //阻塞一毫秒
                    currTimestamp++;
                    _seq = 0;
                }
            }
            else //LT，发生时间回拨
            {
                _cb++;
                if (_cb > 7) //超出了最大回拨次数
                    throw new Exception($"Out of max clock adjustments({_cb})");
            }

            _lastTimestamp = currTimestamp;

            return (_cb << 60)
                   | (currTimestamp << 20)
                   | (_instanceId << 12)
                   | _seq;
        }
    }
}