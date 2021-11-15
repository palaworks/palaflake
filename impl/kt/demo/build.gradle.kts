import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
    kotlin("jvm") version "1.5.10"
    application
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


tasks.withType<org.jetbrains.kotlin.gradle.tasks.KotlinCompile>() {
    kotlinOptions.jvmTarget = "1.8"
}

application {
    mainClass.set("MainKt")
}