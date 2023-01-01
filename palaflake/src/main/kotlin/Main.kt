package palaflake

import java.text.SimpleDateFormat
import java.util.*

/**
 * 构造palaflake生成器
 * @param instanceId 实例ID
 * @param startYear 计时起始年
 */
class Generator(private val instanceId: Byte, private val startYear: UShort) {

    val utcZone = TimeZone.getTimeZone("UTC")

    private val startTimestamp = run {
        val utcNowYear = Calendar.getInstance(utcZone).get(Calendar.YEAR).toUShort()

        //设置了未来时间
        if (utcNowYear < startYear)
            throw Exception("The startYear($startYear) cannot be set to a future time")
        //时间戳溢出
        if (utcNowYear - startYear >= 34u)
            throw Exception("The startYear($startYear) cannot older than 34 years")

        val sdf = SimpleDateFormat("yyyy-MM-dd HH:mm:ss")
        sdf.timeZone = utcZone

        val cal = Calendar.getInstance()
        cal.time = sdf.parse("$startYear-01-01 01:00:00")
        cal.timeInMillis
    }

    private val instanceIdInI64 = instanceId.toLong()
    private var lastTimestamp = 0L
    private var cb = 0L //回拨次数
    private var seq = 0L //序列号

    //ID结构参考
    //01112222 22222222 22222222 22222222
    //22222222 22223333 33334444 44444444

    @Synchronized
    fun next(): Long {
        val utcNowTimestamp = Calendar.getInstance(utcZone).timeInMillis

        //当前时间早于起始时间
        if (utcNowTimestamp < startTimestamp)
            throw Exception("Illegal system time(${Calendar.getInstance(utcZone)})")

        var currTimestamp = utcNowTimestamp - startTimestamp

        when {
            currTimestamp > lastTimestamp -> seq = 0
            currTimestamp == lastTimestamp -> {
                seq++
                if (seq > 4095) //一毫秒内的请求超过4096次
                {
                    Thread.sleep(1)//阻塞一毫秒
                    currTimestamp++
                    seq = 0
                }
            }
            else -> {//LT，发生时间回拨
                cb++

                //TODO 此处必须阻塞一毫秒（或进行其他耗时操作），否则上面的cb++会有几率自增2
                //Thread.sleep(1)

                if (cb > 7)//超出了最大回拨次数
                    throw Exception("Out of max clock adjustments($cb)")
            }
        }

        lastTimestamp = currTimestamp

        return (cb shl 60) or (currTimestamp shl 20) or (instanceIdInI64 shl 12) or seq
    }
}
