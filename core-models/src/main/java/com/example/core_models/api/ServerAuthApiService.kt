package com.example.core_models.api

import com.example.core_models.dto.*
import retrofit2.Response
import retrofit2.http.*

interface ServerAuthApiService {
    @POST("api/Auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<ApiResponse<AuthResponse>>

    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<ApiResponse<AuthResponse>>

    @POST("api/Auth/forgot-password")
    suspend fun forgotPassword(@Body request: ForgotPasswordRequest): Response<ApiResponse<String>>

    @POST("api/Auth/reset-password")
    suspend fun resetPassword(@Body request: ResetPasswordRequest): Response<ApiResponse<SimpleResponse>>

    @POST("api/Auth/send-otp")
    suspend fun sendOtp(@Body request: SendOtpRequest): Response<ApiResponse<String>>

    @POST("api/Auth/verify-otp")
    suspend fun verifyOtp(@Body request: VerifyOtpRequest): Response<ApiResponse<Boolean>>

    @GET("api/Auth/validate")
    suspend fun validateToken(): Response<ApiResponse<ValidateResponse>>

    @GET("api/Auth/public")
    suspend fun getPublicInfo(): Response<ApiResponse<Map<String, Any>>>

    @POST("api/Auth/send-email-change-otp")
    suspend fun sendEmailChangeOtp(
        @Body request: ChangeEmailRequest
    ): Response<ApiResponse<String>>

    @POST("api/Auth/change-email")
    suspend fun changeEmail(
        @Body request: VerifyOtpRequest
    ): Response<ApiResponse<Boolean>>

    @GET("api/Profile")
    suspend fun getProfile(): Response<ApiResponse<UserResponse>>

    @PUT("api/Profile")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): Response<ApiResponse<UserResponse>>

    @GET("api/Profile/avatars")
    suspend fun getAvailableAvatars(): Response<ApiResponse<List<String>>>

    @POST("api/Profile/complete-registration")
    suspend fun completeRegistration(@Body request: CompleteRegistrationRequest): Response<ApiResponse<UserResponse>>

    @GET("api/Auth/check-username/{username}")
    suspend fun checkUsernameForRegistration(
        @Path("username") username: String
    ): ApiResponse<Boolean>

    @GET("api/Profile/check-username/{username}")
    suspend fun checkUsernameForProfile(
        @Path("username") username: String
    ): ApiResponse<Boolean>

    @PUT("api/Auth/elo/{points}")
    suspend fun updateEloPoints(
        @Path("points") points: Int
    ): Response<ApiResponse<Boolean>>

    @GET("api/Profile/elo")
    suspend fun getEloRating(): Response<ApiResponse<EloInfoResponse>>
}
