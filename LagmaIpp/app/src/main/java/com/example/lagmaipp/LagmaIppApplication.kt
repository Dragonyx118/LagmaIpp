package com.example.lagmaipp

import android.app.Application
import org.osmdroid.config.Configuration
import org.osmdroid.tileprovider.tilesource.TileSourceFactory

class LagmaIppApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        Configuration.getInstance().apply {
            load(this@LagmaIppApplication, getSharedPreferences("osm", MODE_PRIVATE))
            userAgentValue = packageName
            osmdroidTileCache.deleteRecursively()   // ← pulizia forzata, temporanea
        }
    }
}