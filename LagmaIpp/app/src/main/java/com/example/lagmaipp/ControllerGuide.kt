package com.example.lagmaipp

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

data class ControlEntry(val button: String, val action: String)
data class ControlSection(val title: String, val entries: List<ControlEntry>)

val controllerGuide = listOf(
    ControlSection(
        "Sempre attivi (qualsiasi modalità)",
        listOf(
            ControlEntry("Y", "Cicla la modalità in anteprima (generale → jarvis → drone → esplorazione → linea → ...) e fa partire il suo video/audio"),
            ControlEntry("X", "Conferma lo switch alla modalità in anteprima"),
            ControlEntry("Schede (10)", "Ripete il video/audio della modalità in anteprima"),
            ControlEntry("A", "Stop di emergenza (in modalità drone ferma e disarma il drone, altrove ferma il carrello)")
        )
    ),
    ControlSection(
        "Modalità Generale - Contesto AUTO (guida)",
        listOf(
            ControlEntry("Stick SX ↑/↓", "Avanti / indietro"),
            ControlEntry("Stick SX ←/→", "Traslazione laterale sinistra / destra"),
            ControlEntry("Stick DX ←/→", "Rotazione sul posto"),
            ControlEntry("RT", "Velocità +10"),
            ControlEntry("LT", "Velocità -10"),
            ControlEntry("Menu (11)", "Passa al contesto BRACCIO (ferma il carrello)"),
            ControlEntry("Note", "La velocità parte da 100, con minimo 50 e massimo 127")
        )
    ),
    ControlSection(
        "Modalità Generale - Contesto BRACCIO",
        listOf(
            ControlEntry("Stick DX ←/→", "Servo 0 (base), movimento continuo proporzionale allo stick"),
            ControlEntry("Stick SX ←/→", "Servo 1 (spalla), movimento continuo proporzionale allo stick"),
            ControlEntry("D-Pad ↑/↓", "Seleziona il servo tra 2, 3, 4, 5 e 6 (camera), in ciclo"),
            ControlEntry("RT / LT", "Servo selezionato +5° / -5°"),
            ControlEntry("Menu (11)", "Torna al contesto AUTO (ferma il carrello)")
        )
    ),
    ControlSection(
        "Modalità Drone",
        listOf(
            ControlEntry("B", "Arma / disarma (va riarmato ogni volta che entri in modalità drone)"),
            ControlEntry("RT / LT", "Thrust +2000 / -2000 (min 0, max 60000)"),
            ControlEntry("Stick SX ←/→", "Yaw"),
            ControlEntry("Stick DX ←/→", "Roll"),
            ControlEntry("Stick DX ↑/↓", "Pitch"),
            ControlEntry("A", "Stop di emergenza e disarmo")
        )
    )
)

@Composable
fun ControllerGuideDialog(onDismiss: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Comandi del controller") },
        text = {
            LazyColumn(
                modifier = Modifier.heightIn(max = 420.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                items(controllerGuide) { section ->
                    Column {
                        Text(
                            section.title,
                            fontWeight = FontWeight.Bold,
                            fontSize = 15.sp,
                            color = Color(0xFF1E88E5)
                        )
                        Spacer(Modifier.height(6.dp))
                        section.entries.forEach { e ->
                            Row(
                                modifier = Modifier.padding(vertical = 3.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Box(
                                    modifier = Modifier
                                        .widthIn(min = 56.dp)
                                        .background(Color(0xFF10141C), RoundedCornerShape(6.dp))
                                        .padding(horizontal = 8.dp, vertical = 3.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Text(e.button, color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.Bold)
                                }
                                Spacer(Modifier.width(10.dp))
                                Text(e.action, fontSize = 13.sp)
                            }
                        }
                    }
                }
            }
        },
        confirmButton = { Button(onClick = onDismiss) { Text("Chiudi") } }
    )
}