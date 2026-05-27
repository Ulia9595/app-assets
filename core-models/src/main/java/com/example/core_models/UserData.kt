package com.example.core_models

import com.example.core_models.dto.RegisterRequest
import com.example.core_models.dto.UserResponse

data class UserData(
    val name: String = "",
    val email: String = "",
    val uid: String = "",
    val role: String = "player",
    val avatarId: Int? = null,
    val avatarUrl: String? = null,
    val eloPoints: Int = 500
) {
    companion object {
        fun fromServerResponse(response: UserResponse): UserData {
            return UserData(
                name = response.name ?: "",
                email = response.email,
                uid = response.uid,
                role = response.role,
                avatarId = response.avatarId,
                avatarUrl = response.avatarUrl,
                eloPoints = response.eloPoints
            )
        }
    }

    fun toRegisterRequest(password: String, passwordRepeat: String): RegisterRequest {
        return RegisterRequest(
            email = email,
            password = password,
            passwordRepeat = passwordRepeat,
            name = name,
            avatarId = avatarId
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