package com.example.core_models

data class ProfileState(
    val name: String = "",
    val email: String = "",
    val avatarId: Int? = null,
    val avatarUrl: String? = null,
    val isLoading: Boolean = false,
    val error: String? = null,
    val notificationMessage: String? = null,
    val eloPoints: Int = 500,
    val availableAvatars: List<AvatarData> = emptyList(),

    val ratingHistory: List<RatingHistoryItem> = emptyList(),
    val ratingHistoryLoading: Boolean = false,
    val ratingHistoryHasMore: Boolean = true,
    val ratingHistoryPage: Int = 1,

    val leaderboard: LeaderboardData? = null,
    val leaderboardLoading: Boolean = false
) {
    val level: Int get() = eloPoints / 1000
    val pointsInCurrentLevel: Int get() = eloPoints % 1000
    val levelProgress: Float get() = pointsInCurrentLevel / 1000f
    val pointsToNextLevel: Int get() = 1000 - pointsInCurrentLevel
}