package com.example.core_models.repository

import android.net.Uri
import com.example.core_models.ProfileState
import kotlinx.coroutines.flow.Flow

interface ProfileRepository {
    fun getProfileData(): Flow<ProfileState>
    suspend fun uploadAvatar(uri: Uri): Result<String>
    suspend fun getAvailableAvatars(): Result<List<String>>

    suspend fun updateUserName(name: String): Result<Unit>
    suspend fun updateEloPoints(points: Int): Result<Unit>

    suspend fun sendEmailChangeOtp(newEmail: String): Result<String>
    suspend fun verifyEmailChange(newEmail: String, code: String): Result<Unit>
}