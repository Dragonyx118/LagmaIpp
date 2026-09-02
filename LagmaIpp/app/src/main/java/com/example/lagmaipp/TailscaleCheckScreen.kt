package com.example.lagmaipp

import android.content.Intent
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape // <-- Import mancante
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.VpnKey
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.net.toUri // <-- Import KTX per toUri()
import kotlinx.coroutines.delay
import kotlin.time.Duration.Companion.milliseconds

@Composable
fun TailscaleCheckScreen(
    onCheckPassed: () -> Unit
) {
    val context = LocalContext.current
    var isChecking by remember { mutableStateOf(true) }
    var isConnected by remember { mutableStateOf(false) }

    val infiniteTransition = rememberInfiniteTransition(label = "pulse")
    val scale by infiniteTransition.animateFloat(
        initialValue = 0.9f,
        targetValue = 1.15f,
        animationSpec = infiniteRepeatable(
            animation = tween(800, easing = FastOutSlowInEasing),
            repeatMode = RepeatMode.Reverse
        ),
        label = "scale"
    )

    LaunchedEffect(Unit) {
        while (true) {
            isChecking = true
            delay(1200.milliseconds)

            val connected = TailscaleChecker.isVpnConnected(context)
            isConnected = connected
            isChecking = false

            if (connected) {
                delay(800.milliseconds)
                onCheckPassed()
                break
            }

            delay(2000.milliseconds)
        }
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color(0xFF10141C)),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
            modifier = Modifier.padding(24.dp)
        ) {
            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(120.dp)
                    .scale(if (isChecking) scale else 1.0f)
                    .background(
                        color = when {
                            isChecking -> Color(0xFF1E88E5).copy(alpha = 0.2f)
                            isConnected -> Color(0xFF2ED573).copy(alpha = 0.2f)
                            else -> Color(0xFFE53935).copy(alpha = 0.2f)
                        },
                        shape = CircleShape
                    )
            ) {
                Icon(
                    imageVector = when {
                        isConnected -> Icons.Default.CheckCircle
                        !isChecking -> Icons.Default.Warning
                        else -> Icons.Default.VpnKey
                    },
                    contentDescription = "Tailscale Status",
                    modifier = Modifier.size(60.dp),
                    tint = when {
                        isConnected -> Color(0xFF2ED573)
                        !isChecking -> Color(0xFFE53935)
                        else -> Color(0xFF1E88E5)
                    }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))

            Text(
                text = when {
                    isChecking -> "Verifica rete Tailscale..."
                    isConnected -> "Connessione Tailscale Rilevata Good boy!"
                    else -> "Rete Tailscale non trovata idiot"
                },
                fontSize = 20.sp,
                fontWeight = FontWeight.Bold,
                color = Color.White,
                textAlign = TextAlign.Center
            )

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = when {
                    isChecking -> "Sto controllando la connessione con i moduli di LagmaBills..."
                    isConnected -> "Accesso sicuro garantito. Reindirizzamento..."
                    else -> "Per comunicare con LagmaBills devi essere connesso alla rete VPN Tailscale."
                },
                fontSize = 14.sp,
                color = Color.LightGray,
                textAlign = TextAlign.Center,
                modifier = Modifier.padding(horizontal = 16.dp)
            )

            Spacer(modifier = Modifier.height(36.dp))

            if (isChecking) {
                CircularProgressIndicator(
                    color = Color(0xFF1E88E5),
                    strokeWidth = 3.dp,
                    modifier = Modifier.size(36.dp)
                )
            } else if (!isConnected) {
                Button(
                    onClick = {
                        val launchIntent = context.packageManager.getLaunchIntentForPackage("com.tailscale.ipn")
                        if (launchIntent != null) {
                            context.startActivity(launchIntent)
                        } else {
                            val intent = Intent(
                                Intent.ACTION_VIEW,
                                "https://play.google.com/store/apps/details?id=com.tailscale.ipn".toUri()
                            )
                            context.startActivity(intent)
                        }
                    },
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF1E88E5)),
                    shape = RoundedCornerShape(12.dp),
                    modifier = Modifier
                        .fillMaxWidth(0.8f)
                        .height(50.dp)
                ) {
                    Text(
                        text = "Apri App Tailscale",
                        fontSize = 15.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color.White
                    )
                }
            }
        }
    }
}