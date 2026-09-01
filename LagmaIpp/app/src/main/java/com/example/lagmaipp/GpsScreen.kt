package com.example.lagmaipp

import android.content.Context
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import org.osmdroid.tileprovider.tilesource.XYTileSource
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.CustomZoomButtonsController
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker

@Composable
fun GpsScreen() {
    val fix by MqttManager.gps.collectAsState()
    val context = LocalContext.current

    // Nome del file per le preferenze condivise
    val sharedPreferences = remember {
        context.getSharedPreferences("LagmaGpsPrefs", Context.MODE_PRIVATE)
    }

    // 1. Recupera l'ultima posizione salvata (se esiste), altrimenti di default usa Milano (45.4642, 9.1900)
    val savedLat = sharedPreferences.getFloat("LAST_LAT", 45.4642f).toDouble()
    val savedLon = sharedPreferences.getFloat("LAST_LON", 9.1900f).toDouble()

    // Flag per centrare la mappa solo la prima volta che si apre la schermata
    var isInitialCenterDone = remember { false }

    val mapView = remember {
        MapView(context).apply {
            setTileSource(object : XYTileSource(
                "MapTiler",
                0, 19, 256, ".png",
                arrayOf("https://api.maptiler.com/maps/streets-v2/256/"),
                "© OpenStreetMap contributors | MapTiler"
            ) {
                override fun getTileURLString(pMapTileIndex: Long): String {
                    return super.getTileURLString(pMapTileIndex) + "?key=MtbetNN6JVlPKI5bnd9Q"
                }
            })
            setMultiTouchControls(true)
            zoomController.setVisibility(CustomZoomButtonsController.Visibility.NEVER)
            controller.setZoom(15.0)
            // Centra subito sulla posizione salvata precedentemente
            controller.setCenter(GeoPoint(savedLat, savedLon))
        }
    }

    // 2. Ogni volta che arriva un nuovo fix valido, aggiorna la mappa E salva i dati nella memoria persistente
    LaunchedEffect(fix) {
        fix?.let {
            val point = GeoPoint(it.lat, it.lon)

            // Salva in modo permanente su SharedPreferences
            sharedPreferences.edit().apply {
                putFloat("LAST_LAT", it.lat.toFloat())
                putFloat("LAST_LON", it.lon.toFloat())
                apply()
            }

            mapView.overlays.clear()
            val marker = Marker(mapView).apply {
                position = point
                title = "LagmaBills"
            }
            mapView.overlays.add(marker)

            // Centra la mappa automaticamente la prima volta che arriva il fix o se desideri seguirlo
            if (!isInitialCenterDone) {
                mapView.controller.animateTo(point)
                isInitialCenterDone = true
            }
            mapView.invalidate()
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
        AndroidView(factory = { mapView }, modifier = Modifier.fillMaxSize())

        Box(
            modifier = Modifier
                .align(Alignment.BottomStart)
                .fillMaxWidth()
                .background(Color(0xCC10141C))
                .padding(12.dp)
        ) {
            if (fix == null) {
                // Mostra un avviso se non c'è il fix live, ricordando l'ultima posizione memorizzata
                Text("Ultima posizione nota salvata", color = Color.White)
            } else {
                Column {
                    Text("Lat: ${fix!!.lat}  Lon: ${fix!!.lon}", color = Color.White)
                    Text(
                        "Alt: ${fix!!.alt}m  Vel: ${"%.1f".format(fix!!.speed_kn * 1.852)} km/h  Sat: ${fix!!.satellites}",
                        color = Color.White
                    )
                }
            }
        }
    }
}