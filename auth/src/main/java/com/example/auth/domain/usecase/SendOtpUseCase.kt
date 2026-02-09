package com.example.auth.domain.usecase

import com.example.core_models.repository.AuthRepository

class SendOtpUseCase(private val repository: AuthRepository) {
    suspend fun execute(email: String): Result<String> {
        return repository.sendOtpCode(email)
    }
}