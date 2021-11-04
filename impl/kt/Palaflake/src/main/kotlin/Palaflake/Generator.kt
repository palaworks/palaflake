package Palaflake

import java.text.SimpleDateFormat
import java.util.*

/**
 * 构造palaflake生成器
 * @param machineId 机器ID
 * @param startYear 计时起始年
 */
class Generator(private val machineId: Byte, private val startYear: UShort) {

    private val startTimestamp = run {
        val zone = TimeZone.getTimeZone("UTC")
        val utcYear = Calendar.getInstance(zone).get(Calendar.YEAR).toUShort()

        //设置了未来时间
        if (utcYear < startYear)
            throw Exception("The startYear cannot be set to a future time")
        //时间戳溢出
        if (utcYear - startYear >= 34u)
            throw Exception("The startYear cannot older than 34 years")

        val sdf = SimpleDateFormat("yyyy-MM-dd HH:mm:ss")
        sdf.timeZone = zone

        val cal = Calendar.getInstance()
        cal.time = sdf.parse("$startYear-01-01 01:00:00")
        cal.timeInMillis
    }

    private var cb = 0u //回拨次数
    private var seq = 0u //序列号
    private var lastTimestamp = 0uL

    //ID结构参考
    //11111111 00223333 33333333 33333333
    //33333333 33333333 33334444 44444444

    @Synchronized
    fun next(): ULong {
        //当前时间早于起始时间
        if (System.currentTimeMillis() < startTimestamp)
            throw Exception("Abnormal system time")

        var currTimestamp = (System.currentTimeMillis() - startTimestamp).toULong()

        when {
            currTimestamp > lastTimestamp -> seq = 0u
            currTimestamp == lastTimestamp -> {
                seq++
                if (seq == 4096u) //一毫秒内的请求超过4096次
                {
                    Thread.sleep(1)//阻塞一毫秒
                    currTimestamp += 1uL
                    seq = 0u
                }
            }
            currTimestamp < lastTimestamp -> {
                cb++

                //TODO 此处必须阻塞一毫秒（或进行其他耗时操作），否则上面的cb++会有几率自增2
                Thread.sleep(1)

                if (cb >= 4u)//超出了最大回拨次数
                    throw Exception("Too many clock adjustments")
            }
        }

        lastTimestamp = currTimestamp

        return (machineId.toULong() shl 56) or (cb.toULong() shl 52) or (currTimestamp shl 12) or seq.toULong()
    }
}