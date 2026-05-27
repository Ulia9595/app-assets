package com.example.diplom_01

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.core_models.NotificationState
import com.example.data.hubs.NotificationHub
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class NotificationViewModel(
    private val hub: NotificationHub
) : ViewModel() {

    private val _state = MutableStateFlow(NotificationState())

    val state: StateFlow<NotificationState> =
        _state.asStateFlow()

    init {

        observeNotifications()
    }

    private fun observeNotifications() {

        viewModelScope.launch {

            hub.notificationFlow.collect { notification ->

                _state.update { currentState ->

                    val updatedList =
                        listOf(notification) + currentState.notifications

                    currentState.copy(
                        notifications = updatedList,
                        unreadCount = currentState.unreadCount + 1
                    )
                }
            }
        }
    }

    fun clearError() {
        _state.update { it.copy(error = null) }
    }

    fun markAllRead() {
        _state.update { current ->
            current.copy(
                notifications = current.notifications.map { it.copy(isRead = true) },
                unreadCount = 0
            )
        }
    }

    override fun onCleared() {
        super.onCleared()
    }
}