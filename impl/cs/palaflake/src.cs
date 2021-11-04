namespace Palaflake
{
    using System;
    using System.Threading;
    using System.Diagnostics;

    public class Generator
    {
        private DateTime start;

        private ulong machineId;
        private ulong lastTimestamp;

        private byte cb; //回拨次数
        private ushort seq; //序列号

        private object mutex = new();

        //ID结构参考
        //11111111 00223333 33333333 33333333
        //33333333 33333333 33334444 44444444

        /// <summary>
        /// 构造palaflake生成器
        /// </summary>
        /// <param name="machineId">机器ID</param>
        /// <param name="startYear">计时起始年</param>
        public Generator(byte machineId, ushort startYear)
        {
            //设置了未来时间
            Trace.Assert(DateTime.UtcNow.Year >= startYear, "The start_year cannot be set to a future time");
            //时间戳溢出
            Trace.Assert(DateTime.UtcNow.Year - startYear < 34, "The startYear cannot older than 34 years");

            start = new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            this.machineId = machineId;
        }

        public ulong Next()
        {
            lock (mutex)
            {
                var utc = DateTime.UtcNow;

                //当前时间早于起始时间
                Trace.Assert(utc > start, "Abnormal system time");
                var currTimestamp = (ulong) (utc - start).TotalMilliseconds;

                if (currTimestamp > lastTimestamp)
                    seq = 0;
                else if (currTimestamp == lastTimestamp)
                {
                    seq++;
                    if (seq == 4096) //一毫秒内的请求超过4096次
                    {
                        Thread.Sleep(1); //阻塞一毫秒
                        currTimestamp += 1;
                        seq = 0;
                    }
                }
                else
                {
                    cb++;
                    Trace.Assert(cb < 4, "Too many clock adjustments"); //超出了最大回拨次数
                }

                lastTimestamp = currTimestamp;


                return machineId << 56 | (ulong) cb << 52 | currTimestamp << 12 | seq;
            }
        }
    }
}