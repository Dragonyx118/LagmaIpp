package com.example.lagmaipp

import android.content.Intent
import android.provider.Settings
import android.view.InputDevice
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Bluetooth
import androidx.compose.material.icons.filled.Gamepad
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlin.time.Duration.Companion.milliseconds

@Composable
fun ControllerScreen() {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    var connected by remember { mutableStateOf(isGamepadConnected()) }

    // Stato dell'attivazione di LagmaBills
    var isLagmaBillsActive by remember { mutableStateOf(false) }

    var showStartDialog by remember { mutableStateOf(false) }
    var showStopDialog by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        while (true) {
            connected = isGamepadConnected()
            delay(2000.milliseconds)
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 24.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Row(
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.Center
            ) {
                Icon(
                    imageVector = Icons.Default.Gamepad,
                    contentDescription = null,
                    tint = Color(0xFF1E88E5),
                    modifier = Modifier.size(24.dp)
                )
                Spacer(modifier = Modifier.width(8.dp))
                Icon(
                    imageVector = Icons.Default.Bluetooth,
                    contentDescription = null,
                    tint = if (connected) Color(0xFF2ED573) else Color.Gray,
                    modifier = Modifier.size(20.dp)
                )
            }
            Spacer(modifier = Modifier.height(8.dp))
            Text(
                text = if (connected) "🎮 Xbox controller connesso" else "Nessun controller Xbox rilevato (Sony merda)",
                fontSize = 14.sp,
                color = Color.DarkGray
            )
            Spacer(modifier = Modifier.height(12.dp))
            Button(
                onClick = { context.startActivity(Intent(Settings.ACTION_BLUETOOTH_SETTINGS)) },
                contentPadding = PaddingValues(horizontal = 16.dp, vertical = 6.dp)
            ) {
                Text("Impostazioni Bluetooth", fontSize = 12.sp)
            }
        }

        Column(
            modifier = Modifier.fillMaxSize(),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            // Bottone Attiva LagmaBills
            Button(
                onClick = { showStartDialog = true },
                enabled = !isLagmaBillsActive,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFF1E88E5),        // Blu standard (Attivo/Premibile)
                    disabledContainerColor = Color(0xFF0D47A1), // Blu scuro (Già attivato/Disabilitato)
                    disabledContentColor = Color.LightGray
                ),
                modifier = Modifier
                    .width(220.dp)
                    .height(56.dp)
            ) {
                Text(
                    text = if (isLagmaBillsActive) "LagmaBills Attivo" else "Activate LagmaBills",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )
            }

            Spacer(modifier = Modifier.height(16.dp))

            // Bottone Arresta LagmaBills
            Button(
                onClick = { showStopDialog = true },
                enabled = isLagmaBillsActive,
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFF616161),        // Grigio (Premibile)
                    disabledContainerColor = Color(0xFFE0E0E0), // Grigio chiaro (Disabilitato)
                    disabledContentColor = Color.DarkGray
                ),
                modifier = Modifier
                    .width(220.dp)
                    .height(56.dp)
            ) {
                Text(
                    text = "Arresta LagmaBills",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        if (showStartDialog) {
            AlertDialog(
                onDismissRequest = { showStartDialog = false },
                title = { Text("Attivazione LagmaBills") },
                text = { Text("Are u sure?") },
                confirmButton = {
                    Box(modifier = Modifier.fillMaxWidth(), contentAlignment = Alignment.Center) {
                        Button(
                            onClick = {
                                showStartDialog = false
                                isLagmaBillsActive = true
                                scope.launch { UdpCommander.send("START") }
                            },
                            shape = CircleShape,
                            colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFE53935)),
                            modifier = Modifier.size(256.dp)
                        ) {
                            Text("Pretty sure!", color = Color.White, fontWeight = FontWeight.Bold, fontSize = 32.sp)
                        }
                    }
                }
            )
        }

        if (showStopDialog) {
            AlertDialog(
                onDismissRequest = { showStopDialog = false },
                title = { Text("Shutdown LagmaBills") },
                text = { Text("Fermare tutti i processi?") },
                confirmButton = {
                    Button(
                        onClick = {
                            showStopDialog = false
                            isLagmaBillsActive = false
                            scope.launch { UdpCommander.send("STOP") }
                        },
                        colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFE53935))
                    ) {
                        Text("STOP IT", color = Color.White)
                    }
                },
                dismissButton = {
                    Button(onClick = { showStopDialog = false }) {
                        Text("Nah")
                    }
                }
            )
        }
    }
}

@Composable
fun ControllerConnectedBadge(modifier: Modifier = Modifier) {
    var connected by remember { mutableStateOf(isGamepadConnected()) }

    LaunchedEffect(Unit) {
        while (true) {
            connected = isGamepadConnected()
            delay(2000.milliseconds)
        }
    }

    if (connected) {
        Row(
            modifier = modifier
                .background(
                    Color(0xCC10141C),
                    RoundedCornerShape(topEnd = 20.dp, bottomEnd = 20.dp)
                )
                .padding(horizontal = 12.dp, vertical = 6.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = Icons.Default.Bluetooth,
                contentDescription = "Controller connesso",
                tint = Color(0xFF2ED573),
                modifier = Modifier.size(16.dp)
            )
            Spacer(modifier = Modifier.width(6.dp))
            Text("Controller", color = Color.White, fontSize = 12.sp)
        }
    }
}

private fun isGamepadConnected(): Boolean {
    return InputDevice.getDeviceIds().any { id ->
        val device = InputDevice.getDevice(id)
        device != null && (device.sources and InputDevice.SOURCE_GAMEPAD) == InputDevice.SOURCE_GAMEPAD
    }
}