namespace palaflake

open System
open System.Threading
open System.Diagnostics

type private Ordering =
    | GT
    | EQ
    | LT

type Generator(machineId: byte, startYear: uint16) =
    //暂不考虑State Monad

    let start =
        Debug.Assert(DateTime.UtcNow.Year - int startYear < 34, "The startYear cannot older than 34 years")
        DateTime(int startYear, 1, 1, 0, 0, 0, DateTimeKind.Utc)

    let mutable lastTimestamp = 0UL

    let mutable cb = 0uy //回拨次数
    let mutable seq = 0us //序列号

    let syncLock = Object()

    let compare a b : Ordering =
        if a > b then GT
        else if a = b then EQ
        else LT

    //ID结构参考
    //11111111 11111111 11111111 11111111
    //11111111 22222222 00445555 55555555

    member this.Next() =
        lock syncLock
        <| fun _ ->
            let mutable currTimestamp =
                uint64 (DateTime.UtcNow - start).TotalMilliseconds

            match compare currTimestamp lastTimestamp with
            | GT -> seq <- 0us
            | EQ ->
                seq <- seq + 1us

                if seq = 4096us //一毫秒内的请求超过4096次
                then
                    Thread.Sleep 1 //阻塞一毫秒
                    currTimestamp <- currTimestamp + 1UL
                    seq <- 0us

            | LT ->
                cb <- cb + 1uy
                Trace.Assert(cb < 4uy, "Too many clock adjustments") //超出了最大回拨次数

            lastTimestamp <- currTimestamp

            (currTimestamp <<< 24)
            ||| (uint64 machineId <<< 16)
            ||| (uint64 cb <<< 12)
            ||| uint64 seq
