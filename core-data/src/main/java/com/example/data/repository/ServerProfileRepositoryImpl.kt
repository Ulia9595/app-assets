package com.example.data.repository

import android.content.Context
import android.net.Uri
import com.example.core_models.ProfileState
import com.example.core_models.api.ServerAuthApiService
import com.example.core_models.dto.ChangeEmailRequest
import com.example.core_models.dto.EloInfoResponse
import com.example.core_models.dto.UpdateProfileRequest
import com.example.core_models.dto.VerifyOtpRequest
import com.example.core_models.repository.ProfileRepository
import com.example.data.TokenManager
import com.example.data.network.HttpClientFactory
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.channels.awaitClose
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.callbackFlow
import kotlinx.coroutines.withContext
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import com.example.core_models.AvatarData
import com.example.core_models.RatingHistoryItem
import com.example.data.AppConfig
import com.example.core_models.LeaderboardData
import com.example.core_models.dto.LeaderboardDataDto

class ServerProfileRepositoryImpl(private val context: Context) : ProfileRepository {
    private val tokenManager = TokenManager(context)
    private val gson = Gson()

    private val apiService: ServerAuthApiService by lazy {
        createApiService()
    }

    private fun createApiService(): ServerAuthApiService {
        val client = HttpClientFactory.createUnsafeOkHttpClient()

        val retrofit = Retrofit.Builder()
            .baseUrl("${AppConfig.serverUrl}/")
            .client(client)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()

        return retrofit.create(ServerAuthApiService::class.java)
    }

    override suspend fun updateEloPoints(points: Int): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.updateEloPoints(points)
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse != null && apiResponse.success == true) {
                    Result.success(Unit)
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка обновления ELO"))
                }
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка обновления ELO: ${e.message}"))
        }
    }

    suspend fun getEloRating(): Result<EloInfoResponse> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getEloRating()
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse != null && apiResponse.success == true) {
                    apiResponse.data?.let { Result.success(it) }
                        ?: Result.failure(Exception("Данные ELO отсутствуют"))
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка получения ELO"))
                }
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка получения ELO: ${e.message}"))
        }
    }

    override fun getProfileData(): Flow<ProfileState> = callbackFlow {
        try {
            val profileResponse = withContext(Dispatchers.IO) { apiService.getProfile() }

            if (profileResponse.isSuccessful) {
                val apiResponse = profileResponse.body()
                if (apiResponse != null && apiResponse.success == true) {
                    val userResponse = apiResponse.data
                    if (userResponse != null) {
                        val eloResult = withContext(Dispatchers.IO) { getEloRating() }
                        val eloPoints = eloResult.getOrNull()?.eloPoints ?: userResponse.eloPoints

                        trySend(
                            ProfileState(
                                name = userResponse.name ?: "",
                                email = userResponse.email,
                                avatarId = userResponse.avatarId,
                                avatarUrl = userResponse.avatarUrl,
                                eloPoints = eloPoints,
                                isLoading = false,
                                error = null
                            )
                        )
                    } else {
                        trySend(ProfileState(error = "Данные пользователя не получены"))
                    }
                } else {
                    trySend(ProfileState(error = apiResponse?.error ?: "Ошибка получения профиля"))
                }
            } else {
                val errorBody = profileResponse.errorBody()?.string() ?: profileResponse.message()
                trySend(ProfileState(error = "Ошибка ${profileResponse.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            trySend(ProfileState(error = "Ошибка сети: ${e.message}"))
        }
        awaitClose()
    }

    override suspend fun getAvailableAvatars(): Result<List<AvatarData>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getAvailableAvatars()

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null && apiResponse.success == true) {
                    val avatars = apiResponse.data
                        ?.map {
                            AvatarData(
                                id = it.id,
                                url = it.url,
                                displayOrder = it.displayOrder
                            )
                        }
                        ?: emptyList()

                    Result.success(avatars)
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка получения аватаров"))
                }
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка сети: ${e.message}"))
        }
    }

    override suspend fun uploadAvatar(uri: Uri): Result<String> {
        return Result.failure(Exception("Загрузка аватаров не поддерживается. Выберите аватар из списка."))
    }

    suspend fun updateUserAvatar(avatarId: Int): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val request = UpdateProfileRequest(avatarId = avatarId)
            val response = apiService.updateProfile(request)

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null && apiResponse.success == true) {
                    Result.success(Unit)
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка обновления аватара"))
                }
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка сети: ${e.message}"))
        }
    }

    override suspend fun updateUserName(name: String): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val checkResponse = apiService.checkUsernameForProfile(name)

            if (!checkResponse.isSuccessful) {
                val errorMessage = parseApiError(checkResponse.errorBody()?.string())
                    ?: "Ошибка проверки имени"
                return@withContext Result.failure(Exception(errorMessage))
            }

            val checkApiResponse = checkResponse.body()

            if (checkApiResponse?.success == true && checkApiResponse.data == true) {
                val updateRequest = UpdateProfileRequest(name = name)
                val updateResponse = apiService.updateProfile(updateRequest)

                if (updateResponse.isSuccessful) {
                    val updateApiResponse = updateResponse.body()

                    if (updateApiResponse?.success == true) {
                        Result.success(Unit)
                    } else {
                        Result.failure(Exception(updateApiResponse?.error ?: "Ошибка обновления имени"))
                    }
                } else {
                    val errorMessage = parseApiError(updateResponse.errorBody()?.string())
                        ?: "Ошибка обновления профиля"
                    Result.failure(Exception(errorMessage))
                }
            } else {
                Result.failure(Exception(checkApiResponse?.error ?: "Это имя уже занято"))
            }
        } catch (e: Exception) {
            Result.failure(Exception(e.message ?: "Ошибка сети"))
        }
    }

    private fun parseApiError(errorBody: String?): String? {
        if (errorBody.isNullOrBlank()) return null

        return try {
            val errorResponse = gson.fromJson(errorBody, com.example.core_models.dto.ApiResponse::class.java)
            errorResponse?.error
        } catch (e: Exception) {
            null
        }
    }

    override suspend fun sendEmailChangeOtp(newEmail: String): Result<String> = withContext(Dispatchers.IO) {
        try {
            val request = ChangeEmailRequest(newEmail = newEmail)
            val response = apiService.sendEmailChangeOtp(request) // токен подставляется автоматически
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse?.success == true) Result.success(apiResponse.data ?: "Код отправлен на новый email")
                else Result.failure(Exception(apiResponse?.error ?: "Ошибка отправки кода"))
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка сети: ${e.message}"))
        }
    }

    override suspend fun verifyEmailChange(newEmail: String, code: String): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val request = VerifyOtpRequest(email = newEmail, code = code)
            val response = apiService.changeEmail(request) // токен подставляется автоматически
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse?.success == true) Result.success(Unit)
                else Result.failure(Exception(apiResponse?.error ?: "Ошибка смены email"))
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка сети: ${e.message}"))
        }
    }

    suspend fun getRatingHistory(page: Int, pageSize: Int): Result<List<RatingHistoryItem>> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.getRatingHistory(page = page, pageSize = pageSize)
                if (response.isSuccessful) {
                    val apiResponse = response.body()
                    if (apiResponse?.success == true) {
                        val items = apiResponse.data?.map { it.toDomain() } ?: emptyList()
                        Result.success(items)
                    } else {
                        Result.failure(Exception(apiResponse?.error ?: "Ошибка загрузки истории"))
                    }
                } else {
                    val errorBody = response.errorBody()?.string() ?: response.message()
                    Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Ошибка сети: ${e.message}"))
            }
        }

    suspend fun getLeaderboard(): Result<LeaderboardData> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getLeaderboard()
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse?.success == true) {
                    val dto: LeaderboardDataDto = apiResponse.data
                        ?: return@withContext Result.failure(Exception("Данные рейтинга отсутствуют"))
                    Result.success(dto.toDomain())
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка загрузки рейтинга"))
                }
            } else {
                val errorBody = response.errorBody()?.string() ?: response.message()
                Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ошибка сети: ${e.message}"))
        }
    }
}