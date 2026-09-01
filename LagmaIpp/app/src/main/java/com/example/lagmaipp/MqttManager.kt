package com.example.lagmaipp

import com.hivemq.client.mqtt.mqtt3.Mqtt3Client
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json

object MqttManager {

    private const val BROKER_HOST = "100.100.61.49"
    private const val BROKER_PORT = 1883

    private val json = Json { ignoreUnknownKeys = true }

    private val client = Mqtt3Client.builder()
        .identifier("lagmaipp-android-${System.currentTimeMillis()}")
        .serverHost(BROKER_HOST)
        .serverPort(BROKER_PORT)
        .buildAsync()

    private val _piStats = MutableStateFlow(PiStats())
    val piStats: StateFlow<PiStats> = _piStats

    private val _gps = MutableStateFlow<GpsFix?>(null)
    val gps: StateFlow<GpsFix?> = _gps

    private val _connected = MutableStateFlow(false)
    val connected: StateFlow<Boolean> = _connected

    fun connect() {
        client.publishes(com.hivemq.client.mqtt.MqttGlobalPublishFilter.SUBSCRIBED) { pub ->
            val topic = pub.topic.toString()
            val payload = String(pub.payloadAsBytes, Charsets.UTF_8)

            when (topic) {
                "robot/pi/stats" -> {
                    runCatching { json.decodeFromString<PiStats>(payload) }
                        .onSuccess { _piStats.value = it }
                }
                "robot/gps/posizione" -> {
                    runCatching { json.decodeFromString<GpsFix>(payload) }
                        .onSuccess { _gps.value = it }
                }
            }
        }

        client.connectWith().send().whenComplete { _, throwable ->
            val success = throwable == null
            _connected.value = success
            if (success) {
                client.subscribeWith().topicFilter("robot/pi/stats").send()
                client.subscribeWith().topicFilter("robot/gps/posizione").send()
            }
        }
    }

    fun publish(topic: String, payload: String) {
        client.publishWith()
            .topic(topic)
            .payload(payload.toByteArray())
            .send()
    }
}

@Serializable
data class PiStats(
    val cpu_pct: Double = 0.0,
    val ram_pct: Double = 0.0,
    val ram_free_mb: Double = 0.0,
    val temp_c: Double = 0.0
)

@Serializable
data class GpsFix(
    val lat: Double = 0.0,
    val lon: Double = 0.0,
    val alt: Double = 0.0,
    val speed_kn: Float = 0f,
    val satellites: Int = 0
)