package com.example.lagmaipp

import android.Manifest
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.CompassCalibration
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material3.*
import androidx.compose.material3.adaptive.navigationsuite.NavigationSuiteDefaults
import androidx.compose.material3.adaptive.navigationsuite.NavigationSuiteScaffold
import androidx.compose.runtime.*
import androidx.compose.runtime.saveable.rememberSaveable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.SpanStyle
import androidx.compose.ui.text.buildAnnotatedString
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.withStyle
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.lagmaipp.ui.theme.LagmaIppTheme
import kotlinx.coroutines.delay
import kotlin.time.Duration.Companion.milliseconds

class MainActivity : ComponentActivity() {

    private val bluetoothPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()

        MqttManager.connect()

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            bluetoothPermissionLauncher.launch(
                arrayOf(
                    Manifest.permission.BLUETOOTH_CONNECT,
                    Manifest.permission.BLUETOOTH_SCAN
                )
            )
        }

        setContent {
            LagmaIppTheme {
                var showSplash by remember { mutableStateOf(true) }

                LaunchedEffect(Unit) {
                    delay(2500.milliseconds)
                    showSplash = false
                }

                if (showSplash) {
                    SplashScreen()
                } else {
                    LagmaIppApp()
                }
            }
        }
    }
}

@Composable
fun SplashScreen() {
    val offsetX = remember { Animatable(300f) }

    LaunchedEffect(Unit) {
        offsetX.animateTo(
            targetValue = 0f,
            animationSpec = tween(durationMillis = 1200)
        )
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF1E88E5)),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.offset(x = offsetX.value.dp)
        ) {
            Text(text = "🚓", fontSize = 72.sp)
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = buildAnnotatedString {
                    withStyle(style = SpanStyle(color = Color(0xFF0D47A1), fontWeight = FontWeight.Bold)) {
                        append("L")
                    }
                    append("AGMA")
                    withStyle(style = SpanStyle(color = Color(0xFF0D47A1), fontWeight = FontWeight.Bold)) {
                        append("B")
                    }
                    append("ILLS")
                },
                fontSize = 28.sp,
                fontWeight = FontWeight.Bold,
                color = Color.Black
            )
        }
    }
}

enum class AppDestinations(
    val label: String,
    val iconRes: Int, // Usiamo un Int per l'ID della risorsa drawable
) {
    AI("A.I.", android.R.drawable.ic_menu_compass),
    CONTROLLER("Controller", R.drawable.ic_lagmabills),
    CAMERA("Camera", android.R.drawable.ic_menu_camera),
    GPS("GPS", android.R.drawable.ic_menu_mylocation),
}

@Composable
fun LagmaIppApp() {
    var currentDestination by rememberSaveable { mutableStateOf(AppDestinations.CONTROLLER) }

    val customColors = NavigationSuiteDefaults.colors(
        navigationBarContainerColor = Color(0xFF1E88E5),
        navigationBarContentColor = Color.White
    )

    Scaffold(
        topBar = { StatsTopBar() }
    ) { topBarPadding ->
        Box(modifier = Modifier.padding(topBarPadding)) {
            NavigationSuiteScaffold(
                navigationSuiteColors = customColors,
                navigationSuiteItems = {
                    AppDestinations.entries.forEach { destination ->
                        item(
                            icon = {
                                val iconSize = if (destination == AppDestinations.CONTROLLER) 34.dp else 24.dp
                                Icon(
                                    painter = painterResource(destination.iconRes),
                                    contentDescription = destination.label,
                                    modifier = Modifier.size(iconSize) // ← Usa la variabile iconSize calcolata
                                )
                            },
                            label = { Text(destination.label) },
                            selected = destination == currentDestination,
                            onClick = { currentDestination = destination }
                        )
                    }
                }
            ) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(Color.White)
                ) {
                    when (currentDestination) {
                        AppDestinations.AI -> AiScreen()
                        AppDestinations.CONTROLLER -> ControllerScreen()
                        AppDestinations.CAMERA -> CameraScreen()
                        AppDestinations.GPS -> GpsScreen()
                    }
                }
            }

            ControllerConnectedBadge(
                modifier = Modifier
                    .align(Alignment.BottomStart)
                    .padding(start = 0.dp, bottom = 160.dp)
            )
        }
    }
}

@Composable
fun AiScreen() {
    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
    ) {
        // Immagine di sfondo posizionata sotto il contenuto
        androidx.compose.foundation.Image(
            painter = painterResource(id = R.drawable.jarvis), // Sostituisci con il nome della tua immagine in drawable
            contentDescription = "Sfondo AI",
            modifier = Modifier.fillMaxSize(),
            contentScale = androidx.compose.ui.layout.ContentScale.Crop // Riempie lo schermo mantenendo le proporzioni
        )

        // Contenuto sopra l'immagine
        Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = "A.I. — in arrivo (Panelli Gay)",
                color = Color.White, // Modifica il colore se serve contrasto con l'immagine
                fontWeight = FontWeight.Bold,
                fontSize = 18.sp
            )
        }
    }
}