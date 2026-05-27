package com.example.core_models

data class LeaderboardEntry(
    val userId: Int,
    val rank: Int,
    val name: String,
    val eloPoints: Int,
    val avatarUrl: String?
)

data class LeaderboardData(
    val entries: List<LeaderboardEntry>,
    val currentUserRank: Int
)