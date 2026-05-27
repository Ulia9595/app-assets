package com.example.diplom_01

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.data.hubs.NotificationHub

class NotificationViewModelFactory(
    private val hub: NotificationHub
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(
        modelClass: Class<T>
    ): T {

        if (modelClass.isAssignableFrom(NotificationViewModel::class.java)) {

            @Suppress("UNCHECKED_CAST")
            return NotificationViewModel(hub) as T
        }

        throw IllegalArgumentException(
            "Unknown ViewModel class"
        )
    }
}