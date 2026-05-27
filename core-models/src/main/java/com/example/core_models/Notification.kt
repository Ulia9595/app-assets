package com.example.core_models

data class Notification(
    val id: Int,
    val title: String,
    val message: String,
    val type: String,
    val referenceId: Int?,
    val isRead: Boolean,
    val createdAt: String
)

data class NotificationDto(
    val title: String,
    val message: String,
    val type: String,
    val referenceId: Int?,
    val createdAt: String
)

enum class NotificationType {
    LEVEL_ADDED, LEVEL_UPDATED, LEVEL_DELETED,
    TOPIC_ADDED, TOPIC_UPDATED, TOPIC_DELETED,
    TASK_ADDED, TASK_UPDATED, TASK_DELETED,
    TOURNAMENT_CREATED, TOURNAMENT_UPDATED, TOURNAMENT_DELETED,
    SYSTEM
}