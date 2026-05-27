plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
    id("com.google.gms.google-services")
}

android {
    namespace = "com.ibaapps.HotelOrderSystem"
    compileSdk = 35

    defaultConfig {
        applicationId = "com.ibaapps.HotelOrderSystem"
        minSdk = 23
        targetSdk = 35
        versionCode = 1
        versionName = "1.0.0"

        buildConfigField("String", "WEB_APP_URL", '"https://example.com/#/staff"')
        buildConfigField("String", "API_BASE_URL", '"https://example.com"')
    }

    buildFeatures {
        buildConfig = true
    }

    buildTypes {
        debug {
            versionNameSuffix = "-debug"
            buildConfigField("String", "WEB_APP_URL", '"https://10.0.2.2:5001/#/staff"')
            buildConfigField("String", "API_BASE_URL", '"https://10.0.2.2:5001"')
        }
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}

dependencies {
    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))
    implementation("com.google.firebase:firebase-messaging")
    implementation("androidx.core:core-ktx:1.15.0")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("androidx.webkit:webkit:1.12.1")
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0")
}
