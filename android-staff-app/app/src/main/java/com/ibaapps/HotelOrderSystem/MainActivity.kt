package com.ibaapps.HotelOrderSystem

import android.Manifest
import android.annotation.SuppressLint
import android.content.pm.PackageManager
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.os.Build
import android.os.Bundle
import android.webkit.WebChromeClient
import android.webkit.WebResourceRequest
import android.webkit.WebView
import android.webkit.WebViewClient
import android.widget.LinearLayout
import android.widget.Switch
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.app.ActivityCompat
import androidx.core.content.ContextCompat
import com.google.firebase.messaging.FirebaseMessaging
import com.ibaapps.HotelOrderSystem.bridge.NativeBridge
import com.ibaapps.HotelOrderSystem.net.ApiClient
import com.ibaapps.HotelOrderSystem.storage.AppPrefs
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {
    private val scope: CoroutineScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)
    private lateinit var prefs: AppPrefs
    private lateinit var api: ApiClient
    private lateinit var webView: WebView
    private lateinit var readySwitch: Switch
    private lateinit var statusText: TextView
    private var heartbeatJob: Job? = null

    @SuppressLint("SetJavaScriptEnabled")
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        prefs = AppPrefs(this)
        api = ApiClient(prefs)

        requestNotificationPermission()

        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
        }

        val bar = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            setPadding(24, 16, 24, 16)
        }

        statusText = TextView(this).apply {
            text = if (prefs.isReady) "Ready for orders" else "Not ready"
            textSize = 14f
            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
        }

        readySwitch = Switch(this).apply {
            text = "Ready"
            isChecked = prefs.isReady
            setOnCheckedChangeListener { _, checked ->
                prefs.isReady = checked
                statusText.text = if (checked) "Ready for orders" else "Not ready"
                scope.launch(Dispatchers.IO) { runCatching { api.setAvailability(checked) } }
                webView.evaluateJavascript("window.dispatchEvent(new CustomEvent('native-ready-changed',{detail:{isReady:$checked}}));", null)
            }
        }

        bar.addView(statusText)
        bar.addView(readySwitch)

        webView = WebView(this).apply {
            settings.javaScriptEnabled = true
            settings.domStorageEnabled = true
            settings.databaseEnabled = true
            settings.setSupportZoom(false)
            settings.mediaPlaybackRequiresUserGesture = false
            webChromeClient = WebChromeClient()
            webViewClient = object : WebViewClient() {
                override fun shouldOverrideUrlLoading(view: WebView, request: WebResourceRequest): Boolean = false
                override fun onPageFinished(view: WebView, url: String) {
                    super.onPageFinished(view, url)
                    injectSessionBridge()
                    syncReadyToWeb()
                }
            }
            addJavascriptInterface(NativeBridge(prefs, api, scope) { ready ->
                runOnUiThread {
                    readySwitch.isChecked = ready
                    statusText.text = if (ready) "Ready for orders" else "Not ready"
                }
            }, "HotelNative")
        }

        root.addView(bar)
        root.addView(webView, LinearLayout.LayoutParams(LinearLayout.LayoutParams.MATCH_PARENT, 0, 1f))
        setContentView(root)

        fetchFcmToken()
        webView.loadUrl(BuildConfig.WEB_APP_URL)
    }

    override fun onResume() {
        super.onResume()
        startHeartbeat()
    }

    override fun onPause() {
        super.onPause()
        heartbeatJob?.cancel()
        scope.launch(Dispatchers.IO) { runCatching { api.heartbeat("background", "staff-webview") } }
    }

    override fun onDestroy() {
        heartbeatJob?.cancel()
        scope.cancel()
        super.onDestroy()
    }

    override fun onBackPressed() {
        if (webView.canGoBack()) webView.goBack() else super.onBackPressed()
    }

    private fun fetchFcmToken() {
        FirebaseMessaging.getInstance().token.addOnSuccessListener { token ->
            prefs.fcmToken = token
            if (!prefs.authToken.isNullOrBlank()) {
                scope.launch(Dispatchers.IO) { runCatching { api.registerDeviceToken(token) } }
            }
        }
    }

    private fun startHeartbeat() {
        heartbeatJob?.cancel()
        heartbeatJob = scope.launch {
            while (true) {
                if (!prefs.authToken.isNullOrBlank()) {
                    launch(Dispatchers.IO) { runCatching { api.heartbeat("foreground", "staff-webview") } }
                }
                delay(60_000)
            }
        }
    }

    private fun injectSessionBridge() {
        val js = """
            (function(){
              try {
                const raw = localStorage.getItem('hotel.ops.session');
                if (raw) {
                  const session = JSON.parse(raw);
                  if (session && session.token && window.HotelNative) window.HotelNative.setAuthToken(session.token);
                }
              } catch(e) {}
              window.HotelNativeReady = {
                setReady: function(value){ if(window.HotelNative) window.HotelNative.setReady(!!value); },
                getDeviceId: function(){ return window.HotelNative ? window.HotelNative.getDeviceId() : ''; },
                isReady: function(){ return window.HotelNative ? window.HotelNative.isReady() : false; }
              };
            })();
        """.trimIndent()
        webView.evaluateJavascript(js, null)
    }

    private fun syncReadyToWeb() {
        val ready = prefs.isReady
        webView.evaluateJavascript("window.dispatchEvent(new CustomEvent('native-ready-changed',{detail:{isReady:$ready}}));", null)
    }

    private fun requestNotificationPermission() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
            ContextCompat.checkSelfPermission(this, Manifest.permission.POST_NOTIFICATIONS) != PackageManager.PERMISSION_GRANTED) {
            ActivityCompat.requestPermissions(this, arrayOf(Manifest.permission.POST_NOTIFICATIONS), 1001)
        }
    }

    private fun isOnline(): Boolean {
        val manager = getSystemService(ConnectivityManager::class.java)
        val network = manager.activeNetwork ?: return false
        val capabilities = manager.getNetworkCapabilities(network) ?: return false
        return capabilities.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
    }
}
