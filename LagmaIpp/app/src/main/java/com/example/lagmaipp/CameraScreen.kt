package com.example.lagmaipp

import android.app.Activity
import android.content.pm.ActivityInfo
import android.webkit.WebView
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Fullscreen
import androidx.compose.material.icons.filled.FullscreenExit
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat

private const val CAMERA_STREAM_URL = "http://100.100.61.49:8080/"

@Composable
fun CameraScreen() {
    val context = LocalContext.current
    val activity = context as? Activity
    var isFullscreen by remember { mutableStateOf(false) }

    LaunchedEffect(isFullscreen) {
        activity?.let { act ->
            val window = act.window
            val controller = WindowCompat.getInsetsController(window, window.decorView)
            if (isFullscreen) {
                WindowCompat.setDecorFitsSystemWindows(window, false)
                controller.hide(WindowInsetsCompat.Type.systemBars())
                controller.systemBarsBehavior =
                    WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                act.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE
            } else {
                WindowCompat.setDecorFitsSystemWindows(window, true)
                controller.show(WindowInsetsCompat.Type.systemBars())
                act.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED
            }
        }
    }

    DisposableEffect(Unit) {
        onDispose {
            activity?.let { act ->
                val window = act.window
                WindowCompat.setDecorFitsSystemWindows(window, true)
                WindowCompat.getInsetsController(window, window.decorView)
                    .show(WindowInsetsCompat.Type.systemBars())
                act.requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED
            }
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
        AndroidView(
            factory = { ctx ->
                WebView(ctx).apply {
                    settings.javaScriptEnabled = false
                    loadUrl(CAMERA_STREAM_URL)
                }
            },
            modifier = Modifier.fillMaxSize()
        )

        IconButton(
            onClick = { isFullscreen = !isFullscreen },
            modifier = Modifier
                .align(Alignment.TopEnd)
                .padding(12.dp)
                .background(Color(0x99000000), CircleShape)
        ) {
            Icon(
                imageVector = if (isFullscreen) Icons.Default.FullscreenExit else Icons.Default.Fullscreen,
                contentDescription = if (isFullscreen) "Esci da fullscreen" else "Fullscreen",
                tint = Color.White
            )
        }
    }
}