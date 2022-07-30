namespace palaflake;

using System;
using System.Threading;

public class Generator
{
    private DateTime start;

    private long instanceId;
    private long lastTimestamp;

    private long cb; //回拨次数
    private long seq; //序列号

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

        start = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        this.instanceId = instanceId;
    }

    private object mutex = new();

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
        lock (mutex)
        {
            var utcNow = DateTime.UtcNow;

            if (utcNow < start) //当前时间早于起始时间
                throw new Exception($"Illegal system time({utcNow})");

            var currTimestamp = (long)(utcNow - start).TotalMilliseconds;

            if (currTimestamp > lastTimestamp)
                seq = 0;
            else if (currTimestamp == lastTimestamp)
            {
                seq++;
                if (seq > 4095) //一毫秒内的请求超过4096次
                {
                    Thread.Sleep(1); //阻塞一毫秒
                    currTimestamp++;
                    seq = 0;
                }
            }
            else //LT，发生时间回拨
            {
                cb++;
                if (cb > 7) //超出了最大回拨次数
                    throw new Exception($"Out of max clock adjustments({cb})");
            }

            lastTimestamp = currTimestamp;

            return (cb << 60)
                   | (currTimestamp << 20)
                   | (instanceId << 12)
                   | seq;
        }
    }
}