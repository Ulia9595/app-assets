package com.example.core_models.dto

import com.example.core_models.RatingHistoryItem

data class RatingHistoryItemDto(
    val id: Int = 0,
    val oldRating: Int = 0,
    val newRating: Int = 0,
    val delta: Int = 0,
    val reason: String = "",
    val taskName: String? = null,
    val topicName: String? = null,
    val tournamentId: Int? = null,
    val createdAt: String = ""
) {
    fun toDomain() = RatingHistoryItem(
        id = id,
        oldRating = oldRating,
        newRating = newRating,
        delta = delta,
        reason = reason,
        taskName = taskName,
        topicName = topicName,
        tournamentId = tournamentId,
        createdAt = createdAt
    )
}