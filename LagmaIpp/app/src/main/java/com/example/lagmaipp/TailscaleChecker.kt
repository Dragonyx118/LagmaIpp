package com.example.lagmaipp

import android.content.Context
import android.net.ConnectivityManager
import android.net.NetworkCapabilities

object TailscaleChecker {
    fun isVpnConnected(context: Context): Boolean {
        val connectivityManager =
            context.getSystemService(Context.CONNECTIVITY_SERVICE) as? ConnectivityManager
                ?: return false

        val activeNetwork = connectivityManager.activeNetwork ?: return false
        val capabilities = connectivityManager.getNetworkCapabilities(activeNetwork) ?: return false

        return capabilities.hasTransport(NetworkCapabilities.TRANSPORT_VPN)
    }
}