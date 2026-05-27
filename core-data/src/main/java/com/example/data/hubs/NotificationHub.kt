package com.example.data.hubs

import android.util.Log
import com.example.core_models.Notification
import com.example.core_models.NotificationDto
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.launch
import kotlin.random.Random

class NotificationHub(
    private val serverUrl: String,
    private val tokenProvider: () -> String?
) {

    private var connection: HubConnection? = null

    private val _notificationFlow = MutableSharedFlow<Notification>(
        replay = 1,
        extraBufferCapacity = 10
    )

    val notificationFlow: SharedFlow<Notification> =
        _notificationFlow.asSharedFlow()

    fun connect() {
        if (connection != null) {
            Log.d("NotificationHub", "Already connected")
            return
        }

        val token = tokenProvider()

        if (token == null) {
            Log.e("NotificationHub", "Token is null")
            return
        }

        connection = HubConnectionBuilder
            .create("$serverUrl/hubs/notifications")
            .withAccessTokenProvider(
                Single.defer {
                    Single.just(token)
                }
            )
            .build()

        connection!!.on(
            "ReceiveNotification",
            { dto ->

                Log.d(
                    "NotificationHub",
                    "Notification received: ${dto.title}"
                )

                val notification = Notification(
                    id = Random.nextInt(),
                    title = dto.title,
                    message = dto.message,
                    type = dto.type,
                    referenceId = dto.referenceId,
                    isRead = false,
                    createdAt = dto.createdAt
                )

                CoroutineScope(Dispatchers.IO).launch {
                    _notificationFlow.emit(notification)
                }

            },
            NotificationDto::class.java
        )

        connection!!
            .start()
            .doOnComplete {
                Log.d("NotificationHub", "Connected successfully")
            }
            .doOnError { err ->
                Log.e("NotificationHub", "Connection failed: ${err.message}")
                connection = null
            }
            .subscribe(
                {},
                { err -> Log.e("NotificationHub", "subscribe error: ${err.message}") }
            )
    }

    fun disconnect() {
        connection?.stop()

        connection = null
    }
}