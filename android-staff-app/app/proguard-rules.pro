# Firebase Cloud Messaging
-keep class com.google.firebase.messaging.** { *; }

# App classes (DTOs, domain models, SignalR payloads) used via reflection by
# kotlinx-serialization and the SignalR Gson protocol.
-keep class com.ibaapps.HotelOrderSystem.** { *; }

# ---- kotlinx.serialization ----
-keepattributes *Annotation*, InnerClasses
-dontnote kotlinx.serialization.**
-keepclassmembers class **$$serializer { *; }
-keepclasseswithmembers class * {
    @kotlinx.serialization.Serializable <methods>;
}
-keep,includedescriptorclasses class com.ibaapps.HotelOrderSystem.**$$serializer { *; }
-keepclassmembers class com.ibaapps.HotelOrderSystem.** {
    *** Companion;
}

# ---- Retrofit / OkHttp ----
-keepattributes Signature, Exceptions
-keepattributes RuntimeVisibleAnnotations, RuntimeVisibleParameterAnnotations
-keep,allowobfuscation,allowshrinking interface retrofit2.Call
-keep,allowobfuscation,allowshrinking class retrofit2.Response
-keep,allowobfuscation,allowshrinking class kotlin.coroutines.Continuation
-dontwarn okhttp3.**
-dontwarn okio.**
-dontwarn retrofit2.**

# ---- SignalR (uses RxJava3, Gson, slf4j; reflection on payload classes) ----
-keep class com.microsoft.signalr.** { *; }
-dontwarn com.microsoft.signalr.**
-dontwarn io.reactivex.rxjava3.**
-dontwarn org.slf4j.**

# ---- Gson (SignalR protocol) ----
-keepattributes *Annotation*
-keep class com.google.gson.** { *; }
-dontwarn com.google.gson.**
