package com.example.core_models

data class RatingHistoryItem(
    val id: Int,
    val oldRating: Int,
    val newRating: Int,
    val delta: Int,
    val reason: String,
    val taskName: String?,
    val topicName: String?,
    val tournamentId: Int?,
    val createdAt: String
)
