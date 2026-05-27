package com.example.core_models.dto

data class RegisterRequest(
    val email: String,
    val password: String,
    val passwordRepeat: String,
    val name: String,
    val avatarId: Int? = null
)

data class LoginRequest(
    val email: String,
    val password: String
)

data class SendOtpRequest(
    val email: String,
    val purpose: String = "registration"
)

data class VerifyOtpRequest(
    val email: String,
    val code: String
)

data class ForgotPasswordRequest(
    val email: String
)

data class ResetPasswordRequest(
    val token: String,
    val newPassword: String,
    val confirmPassword: String
)

data class UpdateProfileRequest(
    val name: String? = null,
    val avatarId: Int? = null
)

data class CompleteRegistrationRequest(
    val name: String,
    val avatarId: Int?
)

data class ApiResponse<T>(
    val success: Boolean,
    val data: T? = null,
    val error: String? = null
)

data class AuthResponse(
    val token: String,
    val user: UserResponse
)

data class UserResponse(
    val uid: String = "",
    val email: String = "",
    val role: String = "player",
    val name: String? = null,
    val avatarId: Int? = null,
    val avatarUrl: String? = null,
    val eloPoints: Int = 500
) {
    val level: Int
        get() = eloPoints / 1000

    val pointsInCurrentLevel: Int
        get() = eloPoints % 1000

    val levelProgress: Float
        get() = pointsInCurrentLevel / 1000f

    val pointsToNextLevel: Int
        get() = 1000 - pointsInCurrentLevel

    val isRegistrationComplete: Boolean
        get() = !name.isNullOrEmpty()
}

data class AvatarResponse(
    val id: Int,
    val url: String,
    val displayOrder: Int = 0
)

data class ValidateResponse(
    val isValid: Boolean,
    val userId: Int?,
    val email: String?,
    val uid: String?,
    val role: String? = null
)

data class SimpleResponse(
    val success: Boolean,
    val message: String? = null,
    val error: String? = null
)

data class ChangeEmailRequest(
    val newEmail: String
)

data class ForgotPasswordResponse(
    val message: String? = null,
    val redirectUrl: String? = null
)

data class ChangePasswordRequest(
    val currentPassword: String,
    val newPassword: String,
    val confirmPassword: String
)

data class EloInfoResponse(
    val eloPoints: Int,
    val level: Int,
    val pointsInCurrentLevel: Int,
    val levelProgress: Float,
    val pointsToNextLevel: Int
)