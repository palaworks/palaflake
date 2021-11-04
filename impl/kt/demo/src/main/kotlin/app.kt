import Palaflake.Generator

fun main() {
    val g = Generator(1, 2021u)
    var before = 0UL


    while (true) {
        val latest = g.next()

        if (before >= latest)
            throw Exception()

        println("${latest} : ${showBinary(latest)}")

        before = latest

        //Thread.sleep(233)
    }
}

fun showBinary(timeStamp: ULong) =
    timeStamp
        .toString(2)
        .padStart(64, '0')
        .chunked(8)
        .reduce { a, x -> "$a $x" }