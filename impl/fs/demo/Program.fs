open System
open System.Diagnostics
open System.Threading
open palaflake

let showBinary (timeStamp: uint64) =
    let bytes =
        [ for b in BitConverter.GetBytes(timeStamp) -> b ]

    let rec foldr f acc list =
        match list with
        | x :: xs -> f x (foldr f acc xs)
        | [] -> acc

    let f =
        fun (b: byte) acc -> acc + Convert.ToString(b, 2).PadLeft(8, '0') + " "

    foldr f "" bytes

let g = Generator(1uy, 2021us)
let mutable before = 0UL

while true do

    let latest = g.Next()

    Trace.Assert(before < latest)
    Console.WriteLine($"{latest} : {showBinary latest}")

    before <- latest
