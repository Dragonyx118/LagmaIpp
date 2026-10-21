plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.example.lagmaipp"
    compileSdk {
        version = release(37)
    }

    defaultConfig {
        applicationId = "com.example.lagmaipp"
        minSdk = 24
        targetSdk = 37
        versionCode = 320
        versionName = "3.2.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            optimization {
                enable = false
            }
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    buildFeatures {
        compose = true
    }

    packaging {
        resources {
            excludes += "META-INF/INDEX.LIST"
            excludes += "META-INF/DEPENDENCIES"
            excludes += "META-INF/LICENSE"
            excludes += "META-INF/LICENSE.txt"
            excludes += "META-INF/NOTICE"
            excludes += "META-INF/NOTICE.txt"
            excludes += "META-INF/io.netty.versions.properties"
        }
    }
}

dependencies {
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.material3.adaptive.navigation.suite)
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    testImplementation(libs.junit)
    androidTestImplementation(platform(libs.androidx.compose.bom))
    androidTestImplementation(libs.androidx.compose.ui.test.junit4)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.junit)
    debugImplementation(libs.androidx.compose.ui.test.manifest)
    debugImplementation(libs.androidx.compose.ui.tooling)
    implementation(libs.androidx.compose.material.icons.extended)
    implementation("com.hivemq:hivemq-mqtt-client:1.3.3")
    implementation("org.osmdroid:osmdroid-android:6.1.20")
    implementation("io.coil-kt:coil-compose:2.6.0")
    implementation(libs.kotlinx.serialization.json)
}

val appVersionName = android.defaultConfig.versionName ?: "1.0"
val apkOutputDir = layout.buildDirectory.dir("outputs/apk")

val copyApksTask = tasks.register("copyGeneratedApks") {
    val version = appVersionName
    val buildDirFile = apkOutputDir

    doLast {
        val outputFolder = buildDirFile.get().asFile

        // Cartelle dove copiare gli APK: cambia qui i percorsi
        val cartelleDestinazione = listOf(
            File("C:\\Users\\gianb\\Desktop\\Projects\\LagmaBills\\LagmaIpp\\repo\\LagmaIpp\\Apks")
        )

        cartelleDestinazione.forEach { cartella ->
            if (!cartella.exists()) {
                cartella.mkdirs()
            }
        }

        if (outputFolder.exists()) {
            outputFolder.walkTopDown().forEach { file ->
                // Solo APK veri (> 2 MB), esclude l'APK di androidTest
                if (file.extension == "apk" &&
                    file.length() > 1 * 1024 * 1024 &&
                    !file.name.contains("androidTest")
                ) {
                    val buildType = file.parentFile.name // debug / release
                    val nuovoNomeApk = "LagmaIpp-$buildType-v$version.apk"

                    cartelleDestinazione.forEach { cartella ->
                        val fileDestinazione = File(cartella, nuovoNomeApk)
                        file.copyTo(fileDestinazione, overwrite = true)
                        println("APK copiato in: ${fileDestinazione.absolutePath} (${file.length() / 1024 / 1024} MB)")
                    }
                }
            }
        }
    }
}

tasks.matching {
    it.name.startsWith("assemble") || it.name == "packageDebug" || it.name == "packageRelease"
}.configureEach {
    finalizedBy(copyApksTask)
}

// Usati da eventuali workflow GitHub
tasks.register("printVersionName") { doLast { println(android.defaultConfig.versionName) } }
tasks.register("printVersionCode") { doLast { println(android.defaultConfig.versionCode) } }