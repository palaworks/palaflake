open System
open System.Diagnostics
open System.Threading
open pilipala.util.id.palaflake

let showBinary (timeStamp: int64) =
    let bytes =
        [ for b in BitConverter.GetBytes(timeStamp) -> b ]

    let rec foldr f acc list =
        match list with
        | x :: xs -> f x (foldr f acc xs)
        | [] -> acc

    let f =
        fun (b: byte) acc -> acc + Convert.ToString(b, 2).PadLeft(8, '0') + " "

    foldr f "" bytes


let g = Generator(1uy, 2022us)
let mutable before = 0L

while true do
    //Thread.Sleep 233

    let latest = g.Next()

    Trace.Assert(before < latest)
    Console.WriteLine($"{latest} : {showBinary latest}")

    before <- latest
