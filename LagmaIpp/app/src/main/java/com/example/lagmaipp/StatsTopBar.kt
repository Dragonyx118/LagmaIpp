package com.example.lagmaipp

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.DeveloperBoard
import androidx.compose.material.icons.filled.Memory
import androidx.compose.material.icons.filled.Thermostat
import androidx.compose.material3.Icon
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun StatsTopBar() {
    val stats by MqttManager.piStats.collectAsState()

    val tempColor = when {
        stats.temp_c > 75.0 -> Color(0xFFFF5252)
        stats.temp_c > 60.0 -> Color(0xFFFFC107)
        else -> Color(0xFF2ED573)
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .height(140.dp)
            .background(Color(0xFF1E88E5))
            .padding(horizontal = 12.dp, vertical = 8.dp),
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
                .padding(8.dp)
        ) {
            Text("FREE", color = Color.White.copy(alpha = 0.8f), fontSize = 10.sp, fontWeight = FontWeight.Bold)
            Spacer(modifier = Modifier.height(4.dp))
            Text("${stats.ram_free_mb.toInt()}MB", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
        }

        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier
                .background(Color(0x22FFFFFF), RoundedCornerShape(8.dp))
                .padding(8.dp)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Icon(
                    imageVector = Icons.Default.Thermostat,
                    contentDescription = "Temp",
                    tint = tempColor,
                    modifier = Modifier.size(16.dp)
                )
                Spacer(modifier = Modifier.width(2.dp))
                Text("TEMP", color = Color.White.copy(alpha = 0.8f), fontSize = 10.sp, fontWeight = FontWeight.Bold)
            }
            Spacer(modifier = Modifier.height(4.dp))
            Text(
                text = "${stats.temp_c.toInt()}°C",
                color = tempColor,
                fontSize = 14.sp,
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
            .padding(8.dp)
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Icon(
                imageVector = icon,
                contentDescription = title,
                tint = Color.White,
                modifier = Modifier.size(14.dp)
            )
            Spacer(modifier = Modifier.width(4.dp))
            Text(title, color = Color.White.copy(alpha = 0.8f), fontSize = 10.sp, fontWeight = FontWeight.Bold)
        }
        Spacer(modifier = Modifier.height(4.dp))
        Text(value, color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(6.dp))
        LinearProgressIndicator(
            progress = { progress },
            modifier = Modifier
                .width(48.dp)
                .height(6.dp)
                .clip(RoundedCornerShape(3.dp)),
            color = Color.White,
            trackColor = Color.White.copy(alpha = 0.3f)
        )
    }
}