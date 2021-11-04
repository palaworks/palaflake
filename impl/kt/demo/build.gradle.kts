plugins {
    kotlin("jvm") version "1.5.10"
}

group = "thaumy.cn"
version = "1.0"

repositories {
    mavenCentral()
}

dependencies {
    implementation(fileTree("../Palaflake/build/libs"))
    //implementation(fileTree("src/main/resources"))

    implementation(kotlin("stdlib"))
}