# palaflake

snowflake改进型分布式ID生成方案

## 概述

palaflake意在为低程度分布式场景提供ID生成方案，并通过时间回拨位与保留位为更多场景提供冗余可能。

## ID结构

palaflake同snowflake一样，采用64个二进制位来生成ID，但不同的是palaflake充分利用了第一位（符号位），即palaflake是一个u64值。

下面是一个标准的palaflake二进制表示（间隔和换行是出于排版考虑）：

```text
11111111 00223333 33333333 33333333
33333333 33333333 33334444 44444444
```

* 前8位（被标识为1）：机器标识，最大可供256台设备同时生成ID。

* 第9~10位（被标识为0）：保留，通常置0，但也可用于特别情况下提供冗余的ID空间。

* 第11~12位（被标识为2）：时间回拨位，允许每毫秒至多承受3次时间回拨。

* 第13~52位（被标识为3）：精确到毫秒的时间戳，其最大使用年限为34年。

* 最后12位（被标识为4）：序列号，支持每毫秒生成4096个ID。

## 最佳实践

为充分利用ID空间，请使用从项目启动时间开始的毫秒级时间戳。

palaflake默认支持高达每秒400万（4096000）的并发量，可通过降低前40位时间戳的精确程度来降低palaflake的并发程度以更加充分地利用ID空间（例如每10毫秒增加一次时间戳）。

保留位允许将palaflake的ID空间提升至原来的4倍，但建议的选择是留作应急使用（例如标识因故障而生成了重复ID的机器）。

不建议将保留位用作校验。

## 注意事项

palaflake能够保证在系统在线期间每次都产生递增的ID，但不能保证系统宕机后产生的ID相较于宕机前是递增的。

palaflake的时间回拨位至多允许系统发生三次时间回拨，在这期间依然能够产生唯一且递增的ID。

## 实现

[C#](/impl/cs)  
[F#](/impl/fs)  
[Kotlin](/impl/kt)  
[Rust](/impl/rs)

均包含一份示例代码和一个可直接编译使用的Library。

使用方法：

```kotlin
//以Kotlin为例，其他语言大同小异。
import Palaflake.Generator

fun main() {
    val g = Generator(1, 2021u)//机器标识和计时起始年
    println(g.next())
}
```

* 已知问题
  仅对于Windows10测试环境下的Kotlin实现，在以极高速度生成ID时有概率发生时间回拨位的过多递增（在单线程下也是如此），此问题并不影响生成ID的唯一性与递增性，但会使得容许的时间回拨次数减少。目前在实现中通过引入额外阻塞来限制ID生成速度以屏蔽此问题，但仍不能确定该问题是否会在其他情况下复现，详见Kotlin实现中的`//TODO`标记。其他语言的实现版本并无此问题。另对于macOS12测试环境下，此问题未复现。

## 许可证

The MIT License (MIT)
