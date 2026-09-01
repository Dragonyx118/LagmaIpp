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
import androidx.compose.ui.platform.LocalLifecycleOwner
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import org.osmdroid.config.Configuration
import org.osmdroid.tileprovider.tilesource.XYTileSource
import org.osmdroid.util.GeoPoint
import org.osmdroid.views.CustomZoomButtonsController
import org.osmdroid.views.MapView
import org.osmdroid.views.overlay.Marker

@Composable
fun GpsScreen() {
    val fix by MqttManager.gps.collectAsState()
    val context = LocalContext.current
    val lifecycleOwner = LocalLifecycleOwner.current

    var errorMessage by remember { mutableStateOf<String?>(null) }

    val sharedPreferences = remember {
        context.getSharedPreferences("LagmaGpsPrefs", Context.MODE_PRIVATE)
    }

    // Inizializza la configurazione di Osmdroid (User Agent obbligatorio per evitare blocchi nel caricamento tiles)
    LaunchedEffect(Unit) {
        runCatching {
            Configuration.getInstance().load(context, sharedPreferences)
            Configuration.getInstance().userAgentValue = context.packageName
        }.onFailure { e ->
            errorMessage = "Errore Init OSM: ${e.localizedMessage}"
        }
    }

    val savedLat = sharedPreferences.getFloat("LAST_LAT", 45.4000f).toDouble()
    val savedLon = sharedPreferences.getFloat("LAST_LON", 9.6711f).toDouble()

    var isInitialCenterDone by remember { mutableStateOf(false) }

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
            controller.setZoom(17.0)
            controller.setCenter(GeoPoint(savedLat, savedLon))

            val initialMarker = Marker(this).apply {
                position = GeoPoint(savedLat, savedLon)
                title = "LagmaBills (Ultima nota)"
            }
            overlays.add(initialMarker)
        }
    }

    // Gestione del Lifecycle di Osmdroid per evitare mem-leak o blocchi
    DisposableEffect(lifecycleOwner) {
        val observer = LifecycleEventObserver { _, event ->
            when (event) {
                Lifecycle.Event.ON_RESUME -> mapView.onResume()
                Lifecycle.Event.ON_PAUSE -> mapView.onPause()
                else -> {}
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
            mapView.onDetach()
        }
    }

    // Aggiornamento posizione GPS live
    LaunchedEffect(fix) {
        val currentFix = fix
        if (currentFix != null && currentFix.lat != 0.0 && currentFix.lon != 0.0) {
            runCatching {
                val point = GeoPoint(currentFix.lat, currentFix.lon)

                // Salva le coordinate correnti
                sharedPreferences.edit().apply {
                    putFloat("LAST_LAT", currentFix.lat.toFloat())
                    putFloat("LAST_LON", currentFix.lon.toFloat())
                    apply()
                }

                mapView.overlays.clear()
                val marker = Marker(mapView).apply {
                    position = point
                    title = "LagmaBills (Live)"
                }
                mapView.overlays.add(marker)

                if (!isInitialCenterDone) {
                    mapView.controller.animateTo(point)
                    isInitialCenterDone = true
                } else {
                    // Mantiene la visuale centrata mentre il modulo si sposta
                    mapView.controller.setCenter(point)
                }
                mapView.invalidate()
            }.onFailure { e ->
                errorMessage = "Errore update mappa: ${e.localizedMessage}"
            }
        }
    }

    Box(modifier = Modifier.fillMaxSize()) {
        AndroidView(factory = { mapView }, modifier = Modifier.fillMaxSize())

        // Banner delle informazioni / Errore in basso
        Box(
            modifier = Modifier
                .align(Alignment.BottomStart)
                .fillMaxWidth()
                .background(Color(0xCC10141C))
                .padding(12.dp)
        ) {
            Column {
                if (errorMessage != null) {
                    Text("⚠️ ${errorMessage}", color = Color.Red)
                }

                val currentFix = fix
                if (currentFix == null || (currentFix.lat == 0.0 && currentFix.lon == 0.0)) {
                    Text("In attesa del segnale GPS... (Ultima posizione salvata)", color = Color.Yellow)
                    Text("Lat: $savedLat  Lon: $savedLon", color = Color.Gray)
                } else {
                    Text("Lat: ${currentFix.lat}  Lon: ${currentFix.lon}", color = Color.White)
                    Text(
                        "Alt: ${currentFix.alt}m  Vel: ${"%.1f".format(currentFix.speed_kn * 1.852)} km/h  Sat: ${currentFix.satellites}",
                        color = Color.White
                    )
                }
            }
        }
    }
}