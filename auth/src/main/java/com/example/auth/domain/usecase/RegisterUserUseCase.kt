package com.example.auth.domain.usecase

import com.example.core_models.AuthState
import com.example.core_models.repository.AuthRepository

class RegisterUserUseCase(private val repository: AuthRepository) {
    suspend fun execute(state: AuthState): Result<Unit> {
        return repository.register(state)
    }
}