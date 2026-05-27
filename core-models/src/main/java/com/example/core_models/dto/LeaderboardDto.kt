package com.example.core_models.dto

import com.example.core_models.LeaderboardData
import com.example.core_models.LeaderboardEntry

data class LeaderboardEntryDto(
    val userId: Int = 0,
    val rank: Int = 0,
    val name: String = "",
    val eloPoints: Int = 0,
    val avatarUrl: String? = null
) {
    fun toDomain() = LeaderboardEntry(
        userId = userId,
        rank = rank,
        name = name,
        eloPoints = eloPoints,
        avatarUrl = avatarUrl
    )
}

data class LeaderboardDataDto(
    val entries: List<LeaderboardEntryDto> = emptyList(),
    val currentUserRank: Int = 0
) {
    fun toDomain() = LeaderboardData(
        entries = entries.map { it.toDomain() },
        currentUserRank = currentUserRank
    )
}