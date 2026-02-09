package com.example.core_models

import com.example.core_models.dto.RegisterRequest
import com.example.core_models.dto.UserResponse

data class UserData(
    val name: String = "",
    val email: String = "",
    val uid: String = "",
    val avatarUrl: String? = null,
    val eloPoints: Int = 500,
    val isEmailVerified: Boolean = false
) {
    companion object {
        fun fromServerResponse(response: UserResponse): UserData {
            return UserData(
                name = response.name ?: "",
                email = response.email,
                uid = response.uid,
                avatarUrl = response.avatarUrl,
                eloPoints = response.eloPoints,
                isEmailVerified = response.isEmailVerified
            )
        }
    }

    fun toRegisterRequest(password: String, passwordRepeat: String): RegisterRequest {
        return RegisterRequest(
            email = email,
            password = password,
            passwordRepeat = passwordRepeat,
            name = name,
            avatarUrl = avatarUrl
        )
    }

    val level: Int
        get() = eloPoints / 1000

    val pointsInCurrentLevel: Int
        get() = eloPoints % 1000

    val levelProgress: Float
        get() = pointsInCurrentLevel / 1000f

    val pointsToNextLevel: Int
        get() = 1000 - pointsInCurrentLevel
}