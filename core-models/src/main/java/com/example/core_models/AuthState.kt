package com.example.core_models

data class AuthState(
    val isUserAuthenticated: Boolean = false,
    val email: String = "",
    val userName: String = "",
    val selectedAvatarUrl: String? = null,
    val availableAvatars: List<String> = emptyList(),
    val otpCode: String = "",
    val suggestedNames: List<String> = emptyList(),
    val userEnteredOtp: String = "",
    val password: String = "",
    val passwordRepeat: String = "",
    val step: AuthStep = AuthStep.EMAIL,
    val errorMessage: String? = null,
    val isLoading: Boolean = false,
    val token: String? = null,
    val currentUser: UserData? = null,
    val isOtpVerified: Boolean = false
)

enum class AuthStep {
    EMAIL, CODE, PASSWORD, NAME_AVATAR, FINISH
}