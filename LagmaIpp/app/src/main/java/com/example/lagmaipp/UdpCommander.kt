package com.example.lagmaipp

import android.util.Log
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetAddress

object UdpCommander {
    private const val TAG = "UdpCommanderDebug"
    private const val PI_HOST = "100.100.61.49"
    private const val PI_PORT = 5555

    suspend fun send(command: String) = withContext(Dispatchers.IO) {
        try {
            Log.d(TAG, "Tentativo di invio comando UDP: $command verso $PI_HOST:$PI_PORT")
            val socket = DatagramSocket()
            val address = InetAddress.getByName(PI_HOST)
            val data = command.toByteArray()
            val packet = DatagramPacket(data, data.size, address, PI_PORT)
            socket.send(packet)
            socket.close()
            Log.i(TAG, "Comando UDP inviato con successo: $command")
        } catch (e: Exception) {
            Log.e(TAG, "Errore durante l'invio del comando UDP ($command): ${e.message}", e)
            e.printStackTrace()
        }
    }
}