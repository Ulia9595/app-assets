package com.example.data.repository

import android.content.Context
import com.example.core_models.AuthState
import com.example.core_models.AuthStep
import com.example.core_models.UserData
import com.example.core_models.api.ServerAuthApiService
import com.example.core_models.dto.ApiResponse
import com.example.core_models.dto.ChangeEmailRequest
import com.example.core_models.dto.EloInfoResponse
import com.example.core_models.dto.ForgotPasswordRequest
import com.example.core_models.dto.LoginRequest
import com.example.core_models.dto.RegisterRequest
import com.example.core_models.dto.SendOtpRequest
import com.example.core_models.dto.VerifyOtpRequest
import com.example.core_models.repository.AuthRepository
import com.example.data.TokenManager
import com.example.data.network.HttpClientFactory
import com.google.gson.Gson
import com.google.gson.reflect.TypeToken
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withContext
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class ServerAuthRepositoryImpl(private val context: Context) : AuthRepository {
    private val tokenManager = TokenManager(context)
    private val prefs = context.getSharedPreferences("auth_prefs", Context.MODE_PRIVATE)
    private val gson = Gson()

    private val apiService: ServerAuthApiService by lazy {
        createApiService()
    }

    private fun createApiService(): ServerAuthApiService {
        val client = HttpClientFactory.createUnsafeOkHttpClient()

        val retrofit = Retrofit.Builder()
            .baseUrl("http://192.168.0.102:5141/")
            .client(client)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()

        return retrofit.create(ServerAuthApiService::class.java)
    }

    override fun saveAuthStep(step: AuthStep) {
        prefs.edit().putString("current_auth_step", step.name).apply()
    }

    override fun getSavedAuthStep(): AuthStep {
        val stepName = prefs.getString("current_auth_step", AuthStep.EMAIL.name)
        return try {
            AuthStep.valueOf(stepName ?: AuthStep.EMAIL.name)
        } catch (e: Exception) {
            AuthStep.EMAIL
        }
    }

    override suspend fun register(state: AuthState): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            val request = RegisterRequest(
                email = state.email,
                password = state.password,
                passwordRepeat = state.passwordRepeat,
                name = state.userName.trim(),
                avatarUrl = state.selectedAvatarUrl
            )

            val response = apiService.register(request)

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null && apiResponse.success == true) {
                    apiResponse.data?.let { authResponse ->

                        tokenManager.saveAccessToken(authResponse.token)

                        val userData = UserData.fromServerResponse(authResponse.user)
                        val userJson = gson.toJson(userData)
                        tokenManager.saveUserData(userJson)

                        tokenManager.saveUserEmail(state.email)

                        val savedToken = tokenManager.getAccessToken()
                        val savedUserData = tokenManager.getUserData()
                        val savedEmail = tokenManager.getUserEmail()

                        if (savedToken != null && savedUserData != null) {
                            return@withContext Result.success(Unit)
                        } else {
                            return@withContext Result.failure(Exception("Ошибка сохранения данных"))
                        }
                    }

                    return@withContext Result.failure(Exception("Данные пользователя не получены"))
                } else {
                    val error = apiResponse?.error ?: "Неизвестная ошибка"
                    return@withContext Result.failure(Exception(error))
                }
            } else {
                val errorBody = try {
                    response.errorBody()?.string() ?: response.message()
                } catch (e: Exception) {
                    "Не удалось прочитать тело ошибки"
                }
                try {
                    val errorJson = gson.fromJson(errorBody, ApiResponse::class.java)
                    val errorMessage = errorJson?.error ?: errorBody
                    return@withContext Result.failure(Exception("Ошибка ${response.code()}: $errorMessage"))
                } catch (e: Exception) {
                    return@withContext Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
                }
            }
        } catch (e: Exception) {
            return@withContext Result.failure(
                Exception("Сетевая ошибка: ${e.message ?: "Неизвестная ошибка"}")
            )
        }
    }

    override suspend fun sendOtpCode(email: String): Result<String> = withContext(Dispatchers.IO) {
        try {
            val response =
                apiService.sendOtp(SendOtpRequest(email = email, purpose = "registration"))

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null) {
                    if (apiResponse.success == true) {
                        tokenManager.saveUserEmail(email)

                        return@withContext Result.success("Код отправлен на email")
                    } else {
                        return@withContext Result.failure(
                            Exception(apiResponse.error ?: "Ошибка отправки OTP")
                        )
                    }
                } else {
                    return@withContext Result.failure(
                        Exception("Пустой ответ от сервера")
                    )
                }
            } else {
                val errorBody = try {
                    response.errorBody()?.string() ?: response.message()
                } catch (e: Exception) {
                    response.message()
                }
                return@withContext Result.failure(
                    Exception("Ошибка ${response.code()}: $errorBody")
                )
            }
        } catch (e: Exception) {
            return@withContext Result.failure(e)
        }
    }

    override suspend fun verifyOtp(email: String, code: String): Result<Boolean> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.verifyOtp(VerifyOtpRequest(email = email, code = code))

                if (response.isSuccessful) {
                    val apiResponse = response.body()

                    if (apiResponse != null) {
                        if (apiResponse.success == true) {
                            return@withContext Result.success(true)
                        } else {
                            return@withContext Result.failure(
                                Exception(apiResponse.error ?: "Неверный код")
                            )
                        }
                    } else {
                        return@withContext Result.failure(
                            Exception("Пустой ответ от сервера")
                        )
                    }
                } else {
                    val errorBody = try {
                        response.errorBody()?.string() ?: response.message()
                    } catch (e: Exception) {
                        response.message()
                    }
                    return@withContext Result.failure(
                        Exception("Ошибка ${response.code()}: $errorBody")
                    )
                }
            } catch (e: Exception) {
                return@withContext Result.failure(e)
            }
        }

    override suspend fun isUsernameUnique(name: String): Result<Boolean> =
        withContext(Dispatchers.IO) {
            try {
                val response = apiService.checkUsernameForRegistration(name)

                if (response.success == true) {
                    Result.success(response.data == true)
                } else {
                    Result.failure(Exception(response.error ?: "Ошибка проверки имени"))
                }
            } catch (e: Exception) {
                Result.failure(e)
            }
        }

    override suspend fun getAvailableAvatars(): Result<List<String>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getAvailableAvatars()

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null) {
                    if (apiResponse.success == true) {
                        return@withContext Result.success(apiResponse.data ?: emptyList())
                    } else {
                        return@withContext Result.failure(
                            Exception(apiResponse.error ?: "Ошибка получения аватаров")
                        )
                    }
                } else {
                    return@withContext Result.failure(
                        Exception("Пустой ответ от сервера")
                    )
                }
            } else {
                val errorBody = try {
                    response.errorBody()?.string() ?: response.message()
                } catch (e: Exception) {
                    response.message()
                }
                return@withContext Result.failure(
                    Exception("Ошибка ${response.code()}: $errorBody")
                )
            }
        } catch (e: Exception) {
            return@withContext Result.failure(e)
        }
    }

    override suspend fun completeRegistration(name: String, avatarUrl: String?): Result<Unit> {
        return Result.success(Unit)
    }

    override fun isUserLoggedIn(): Boolean {
        return runBlocking {
            tokenManager.isLoggedIn()
        }
    }

    override suspend fun signOut(): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            tokenManager.clearAll()
            prefs.edit().clear().apply()
            Result.success(Unit)
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override fun getCurrentUser(): UserData? {
        return try {
            val userJson = runBlocking {
                tokenManager.getUserData()
            }
            userJson?.let { gson.fromJson(it, UserData::class.java) }
        } catch (e: Exception) {
            null
        }
    }

    override suspend fun login(email: String, password: String): Result<Unit> =
        withContext(Dispatchers.IO) {
            try {
                val request = LoginRequest(email = email, password = password)
                val response = apiService.login(request)

                if (response.isSuccessful) {
                    val apiResponse = response.body()

                    if (apiResponse != null) {
                        if (apiResponse.success) {
                            apiResponse.data?.let { authResponse ->
                                tokenManager.saveAccessToken(authResponse.token)

                                val userData = UserData(
                                    uid = authResponse.user.uid,
                                    email = authResponse.user.email,
                                    name = authResponse.user.name ?: "",
                                    avatarUrl = authResponse.user.avatarUrl
                                )
                                tokenManager.saveUserData(gson.toJson(userData))
                                tokenManager.saveUserEmail(authResponse.user.email)

                                return@withContext Result.success(Unit)
                            }

                            return@withContext Result.failure(
                                Exception("Данные авторизации не получены")
                            )
                        } else {
                            val errorMsg = apiResponse.error ?: "Неверный email или пароль"
                            return@withContext Result.failure(Exception(errorMsg))
                        }
                    } else {
                        return@withContext Result.failure(Exception("Пустой ответ от сервера"))
                    }
                } else {
                    val errorBody = try {
                        response.errorBody()?.string() ?: response.message()
                    } catch (e: Exception) {
                        response.message()
                    }

                    val errorMessage = try {
                        val errorType = object : TypeToken<ApiResponse<Any>>() {}.type
                        val errorResponse: ApiResponse<Any> = gson.fromJson(errorBody, errorType)
                        errorResponse.error ?: when (response.code()) {
                            401 -> "Неверный email или пароль"
                            404 -> "Пользователь не найден"
                            422 -> "Некорректные данные"
                            400 -> "Неправильный запрос"
                            500 -> "Ошибка сервера"
                            else -> "Ошибка ${response.code()}"
                        }
                    } catch (e: Exception) {
                        when (response.code()) {
                            401 -> "Неверный email или пароль"
                            404 -> "Пользователь не найден"
                            422 -> "Некорректные данные"
                            400 -> "Неправильный запрос"
                            500 -> "Ошибка сервера"
                            else -> "Ошибка ${response.code()}: $errorBody"
                        }
                    }

                    return@withContext Result.failure(Exception(errorMessage))
                }
            } catch (e: Exception) {
                val errorMsg = when {
                    e is java.net.ConnectException -> "Нет подключения к интернету"
                    e is java.net.SocketTimeoutException -> "Превышено время ожидания ответа"
                    e is javax.net.ssl.SSLHandshakeException -> "Ошибка безопасного соединения"
                    e is java.net.UnknownHostException -> "Сервер недоступен"
                    else -> "Сетевая ошибка: ${e.message ?: "Неизвестная ошибка"}"
                }
                return@withContext Result.failure(Exception(errorMsg))
            }
        }

    override suspend fun forgotPassword(email: String): Result<String> =
        withContext(Dispatchers.IO) {
            try {
                val request = ForgotPasswordRequest(email = email)

                val response = apiService.forgotPassword(request)

                if (response.isSuccessful) {
                    val apiResponse = response.body()

                    if (apiResponse != null && apiResponse.success == true) {
                        val message = apiResponse.data ?: "Инструкция отправлена на email"
                        Result.success(message)
                    } else {
                        val error = apiResponse?.error ?: "Неизвестная ошибка"
                        Result.failure(Exception(error))
                    }
                } else {
                    val errorBody = response.errorBody()?.string() ?: response.message()
                    val errorMessage = try {
                        val errorType = object : TypeToken<ApiResponse<Any>>() {}.type
                        val errorResponse: ApiResponse<Any> = gson.fromJson(errorBody, errorType)
                        errorResponse.error ?: "Ошибка ${response.code()}"
                    } catch (e: Exception) {
                        "Ошибка ${response.code()}: $errorBody"
                    }
                    Result.failure(Exception(errorMessage))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Ошибка сети: ${e.message}"))
            }
        }

    override suspend fun changeEmail(newEmail: String): Result<String> =
        withContext(Dispatchers.IO) {
            try {
                val request = ChangeEmailRequest(newEmail = newEmail)
                val response = apiService.sendEmailChangeOtp(request)

                if (response.isSuccessful) {
                    val apiResponse = response.body()
                    if (apiResponse != null && apiResponse.success) {
                        Result.success(apiResponse.data ?: "Код отправлен на новый email")
                    } else {
                        Result.failure(Exception(apiResponse?.error ?: "Ошибка отправки кода"))
                    }
                } else {
                    val errorBody = response.errorBody()?.string() ?: response.message()
                    Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Ошибка сети: ${e.message}"))
            }
        }

    override suspend fun verifyEmailChange(newEmail: String, code: String): Result<Unit> =
        withContext(Dispatchers.IO) {
            try {
                val request = VerifyOtpRequest(email = newEmail, code = code)
                val response = apiService.changeEmail(request)

                if (response.isSuccessful) {
                    val apiResponse = response.body()
                    if (apiResponse != null && apiResponse.success) {
                        Result.success(Unit)
                    } else {
                        Result.failure(Exception(apiResponse?.error ?: "Ошибка смены email"))
                    }
                } else {
                    val errorBody = response.errorBody()?.string() ?: response.message()
                    Result.failure(Exception("Ошибка ${response.code()}: $errorBody"))
                }
            } catch (e: Exception) {
                Result.failure(Exception("Ошибка сети: ${e.message}"))
            }
        }

    suspend fun validateToken(): Result<Boolean> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.validateToken()
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse != null && apiResponse.success) {
                    Result.success(true)
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Токен невалиден"))
                }
            } else {
                Result.failure(Exception("Ошибка валидации: ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun updateEloPoints(points: Int): Result<Boolean> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.updateEloPoints(points)
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse != null && apiResponse.success) {
                    Result.success(apiResponse.data == true)
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка обновления ELO"))
                }
            } else {
                Result.failure(Exception("Ошибка ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    suspend fun getEloRating(): Result<EloInfoResponse> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getEloRating()
            if (response.isSuccessful) {
                val apiResponse = response.body()
                if (apiResponse != null && apiResponse.success) {
                    apiResponse.data?.let { Result.success(it) }
                        ?: Result.failure(Exception("Данные ELO отсутствуют"))
                } else {
                    Result.failure(Exception(apiResponse?.error ?: "Ошибка получения ELO"))
                }
            } else {
                Result.failure(Exception("Ошибка ${response.code()}"))
            }
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    override suspend fun testConnection(): Result<Map<String, Any>> = withContext(Dispatchers.IO) {
        try {
            val response = apiService.getPublicInfo()

            if (response.isSuccessful) {
                val apiResponse = response.body()

                if (apiResponse != null && apiResponse.success == true) {
                    return@withContext Result.success(apiResponse.data ?: emptyMap())
                } else {
                    return@withContext Result.failure(
                        Exception(apiResponse?.error ?: "Ошибка получения информации")
                    )
                }
            } else {
                val errorBody = try {
                    response.errorBody()?.string() ?: response.message()
                } catch (e: Exception) {
                    response.message()
                }
                return@withContext Result.failure(
                    Exception("Ошибка ${response.code()}: $errorBody")
                )
            }
        } catch (e: Exception) {
            return@withContext Result.failure(e)
        }
    }
}