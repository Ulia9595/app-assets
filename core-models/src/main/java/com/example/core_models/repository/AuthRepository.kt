package com.example.core_models.repository

import com.example.core_models.AuthState
import com.example.core_models.AuthStep
import com.example.core_models.AvatarData
import com.example.core_models.UserData

interface AuthRepository {
    suspend fun register(state: AuthState): Result<Unit>
    suspend fun sendOtpCode(email: String): Result<String>
    suspend fun verifyOtp(email: String, code: String): Result<Boolean>
    suspend fun isUsernameUnique(name: String): Result<Boolean>
    suspend fun getAvailableAvatars(): Result<List<AvatarData>>
    suspend fun completeRegistration(name: String, avatarId: Int?): Result<Unit>

    fun isUserLoggedIn(): Boolean
    suspend fun signOut(): Result<Unit>
    fun getCurrentUser(): UserData?

    fun saveAuthStep(step: AuthStep)
    fun getSavedAuthStep(): AuthStep

    suspend fun testConnection(): Result<Map<String, Any>>

    suspend fun login(email: String, password: String): Result<Unit>

    suspend fun forgotPassword(email: String): Result<String>
    suspend fun changeEmail(newEmail: String): Result<String>
    suspend fun verifyEmailChange(newEmail: String, code: String): Result<Unit>

    suspend fun getUsernameSuggestions(): Result<List<String>>
}