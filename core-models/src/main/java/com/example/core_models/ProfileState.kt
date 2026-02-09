package com.example.core_models

data class ProfileState(
    val name: String = "",
    val email: String = "",
    val avatarUrl: String? = null,
    val isLoading: Boolean = false,
    val error: String? = null,
    val notificationMessage: String? = null,
    val eloPoints: Int = 500,
    val availableAvatars: List<String> = emptyList()
) {
    val level: Int get() = eloPoints / 1000

    val pointsInCurrentLevel: Int get() = eloPoints % 1000

    val levelProgress: Float get() = pointsInCurrentLevel / 1000f

    val pointsToNextLevel: Int get() = 1000 - pointsInCurrentLevel
}