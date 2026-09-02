package com.example.lagmaipp

import androidx.compose.animation.animateColorAsState
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DeveloperBoard
import androidx.compose.material.icons.filled.Memory
import androidx.compose.material.icons.filled.Thermostat
import androidx.compose.material.icons.filled.VpnKey
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.delay
import kotlin.time.Duration.Companion.seconds

@Composable
fun StatsTopBar() {
    val stats by MqttManager.piStats.collectAsState()
    val context = LocalContext.current

    var isTailscaleConnected by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        while (true) {
            isTailscaleConnected = TailscaleChecker.isVpnConnected(context)
            delay(2.seconds)
        }
    }

    val tempColor = when {
        stats.temp_c > 75.0 -> Color(0xFFFF5252)
        stats.temp_c > 60.0 -> Color(0xFFFFC107)
        else -> Color(0xFF2ED573)
    }

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .wrapContentHeight()
            .background(Color(0xFF1E88E5))
            .statusBarsPadding() // Rispetta la Status Bar del telefono
            .padding(top = 16.dp, bottom = 8.dp, start = 12.dp, end = 12.dp) // Padding superiore incrementato
    ) {
        // --- Riga Statistiche di Sistema ---
        Row(
            modifier = Modifier.fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            GraphicalStatCard(
                title = "CPU",
                value = "${stats.cpu_pct.toInt()}%",
                progress = (stats.cpu_pct.toFloat() / 100f).coerceIn(0f, 1f),
                icon = Icons.Default.DeveloperBoard
            )

            GraphicalStatCard(
                title = "RAM",
                value = "${stats.ram_pct.toInt()}%",
                progress = (stats.ram_pct.toFloat() / 100f).coerceIn(0f, 1f),
                icon = Icons.Default.Memory
            )

            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier
                    .background(Color(0x22FFFFFF), RoundedCornerShape(8.dp))
                    .padding(6.dp)
            ) {
                Text("MEMORY LEFT", color = Color.White.copy(alpha = 0.8f), fontSize = 9.sp, fontWeight = FontWeight.Bold)
                Spacer(modifier = Modifier.height(2.dp))
                Text("${stats.ram_free_mb.toInt()}MB", color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.Bold)
            }

            Column(
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier
                    .background(Color(0x22FFFFFF), RoundedCornerShape(8.dp))
                    .padding(6.dp)
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        imageVector = Icons.Default.Thermostat,
                        contentDescription = "Temp",
                        tint = tempColor,
                        modifier = Modifier.size(14.dp)
                    )
                    Spacer(modifier = Modifier.width(2.dp))
                    Text("TEMP", color = Color.White.copy(alpha = 0.8f), fontSize = 9.sp, fontWeight = FontWeight.Bold)
                }
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = "${stats.temp_c.toInt()}°C",
                    color = tempColor,
                    fontSize = 12.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        Spacer(modifier = Modifier.height(6.dp))

        // --- Riga Stato Tailscale ---
        TailscaleStatusBarRow(isConnected = isTailscaleConnected)
    }
}

@Composable
private fun TailscaleStatusBarRow(isConnected: Boolean) {
    val statusColor by animateColorAsState(
        targetValue = if (isConnected) Color(0xFF2ED573) else Color(0xFFFF5252),
        label = "tailscaleStatusColor"
    )

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .background(Color(0x22FFFFFF), RoundedCornerShape(8.dp))
            .padding(horizontal = 10.dp, vertical = 5.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = Icons.Default.VpnKey,
                contentDescription = "Tailscale VPN",
                tint = Color.White,
                modifier = Modifier.size(13.dp)
            )
            Spacer(modifier = Modifier.width(6.dp))
            Text(
                text = "TAILSCALE",
                color = Color.White.copy(alpha = 0.9f),
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold
            )
        }

        Row(verticalAlignment = Alignment.CenterVertically) {
            Box(
                modifier = Modifier
                    .size(7.dp)
                    .background(statusColor, CircleShape)
            )
            Spacer(modifier = Modifier.width(5.dp))
            Text(
                text = if (isConnected) "CONNESSO" else "DISCONNESSO",
                color = statusColor,
                fontSize = 10.sp,
                fontWeight = FontWeight.Bold
            )
        }
    }
}

@Composable
private fun GraphicalStatCard(
    title: String,
    value: String,
    progress: Float,
    icon: ImageVector
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier
            .background(Color(0x22FFFFFF), RoundedCornerShape(8.dp))
            .padding(6.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = icon,
                contentDescription = title,
                tint = Color.White,
                modifier = Modifier.size(13.dp)
            )
            Spacer(modifier = Modifier.width(3.dp))
            Text(title, color = Color.White.copy(alpha = 0.8f), fontSize = 9.sp, fontWeight = FontWeight.Bold)
        }
        Spacer(modifier = Modifier.height(2.dp))
        Text(value, color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(4.dp))
        LinearProgressIndicator(
            progress = { progress },
            modifier = Modifier
                .width(44.dp)
                .height(5.dp)
                .clip(RoundedCornerShape(3.dp)),
            color = Color.White,
            trackColor = Color.White.copy(alpha = 0.3f)
        )
    }
}