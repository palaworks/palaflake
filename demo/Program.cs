using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using palaflake;

var g = new Generator(1, 2021);

var before = 0L;

while (true)
{
    var latest = g.Next();

    Trace.Assert(before < latest);
    Console.WriteLine($"{latest} : {showBinary(latest)}");

    before = latest;

    //Thread.Sleep(233);
}

string showBinary(long timeStamp)
{
    StringBuilder sb = new();
    var bytes = BitConverter.GetBytes(timeStamp);
    for (var i = 7; i > -1; --i)
    {
        sb.Append(Convert.ToString(bytes[i], 2).PadLeft(8, '0') + " ");
    }

    return sb.ToString();
}