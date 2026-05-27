package com.example.data.network

import android.content.Context
import android.util.Log
import com.example.data.TokenManager
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.runBlocking
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import java.net.ConnectException
import java.net.SocketTimeoutException
import java.net.UnknownHostException
import java.security.cert.X509Certificate
import java.util.concurrent.TimeUnit
import javax.net.ssl.*

object HttpClientFactory {

    private var tokenManager: TokenManager? = null
    private var unsafeClient: OkHttpClient? = null
    private val _serverAvailable = MutableStateFlow(true)
    val serverAvailable: StateFlow<Boolean> = _serverAvailable.asStateFlow()

    fun initialize(context: Context) {
        tokenManager = TokenManager(context)
        runBlocking {
            tokenManager?.getAccessToken()
        }
    }

    fun createUnsafeOkHttpClient(): OkHttpClient {
        if (unsafeClient == null) {
            unsafeClient = buildUnsafeOkHttpClient()
        }
        return unsafeClient!!
    }

    private fun buildUnsafeOkHttpClient(): OkHttpClient {
        val logging = HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BODY
        }

        return OkHttpClient.Builder()
            .addInterceptor(logging)
            .addInterceptor { chain ->
                val originalRequest = chain.request()
                val requestBuilder = originalRequest.newBuilder()
                    .addHeader("Accept", "application/json")
                    .addHeader("Content-Type", "application/json")

                val token = tokenManager?.let { manager ->
                    runBlocking {
                        try {
                            manager.getAccessToken()
                        } catch (e: Exception) {
                            Log.e("HTTP_INTERCEPTOR", "Ошибка получения токена", e)
                            null
                        }
                    }
                }

                if (token != null && token.isNotBlank()) {
                    val cleanToken = token.trim().removeSurrounding("\"")
                    val authToken = if (cleanToken.startsWith("Bearer ")) cleanToken else "Bearer $cleanToken"
                    requestBuilder.addHeader("Authorization", authToken)
                } else {
                    Log.w("HTTP_INTERCEPTOR", "Токен отсутствует или пустой")
                }

                val request = requestBuilder.build()

                request.headers.forEach { (name, value) ->
                    if (name == "Authorization") {
                        Log.d("HTTP_INTERCEPTOR", "  $name: ${value.take(30)}...")
                    } else {
                        Log.d("HTTP_INTERCEPTOR", "  $name: $value")
                    }
                }

                try {
                    val response = chain.proceed(request)
                    Log.d("HTTP_INTERCEPTOR", "Ответ получен: ${response.code}")
                    _serverAvailable.value = true
                    response
                } catch (e: Exception) {
                    when (e) {
                        is ConnectException,
                        is SocketTimeoutException -> {
                            Log.e("HTTP_INTERCEPTOR", "Сервер недоступен: ${e.message}")
                            _serverAvailable.value = false
                        }
                        is UnknownHostException -> {
                            Log.e("HTTP_INTERCEPTOR", "Хост не найден: ${e.message}")
                        }
                        else -> {
                            Log.e("HTTP_INTERCEPTOR", "Ошибка запроса: ${e.message}")
                        }
                    }
                    throw e
                }
            }
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .apply {
                val trustAllCerts = arrayOf<TrustManager>(object : X509TrustManager {
                    override fun checkClientTrusted(chain: Array<out X509Certificate>?, authType: String?) = Unit
                    override fun checkServerTrusted(chain: Array<out X509Certificate>?, authType: String?) = Unit
                    override fun getAcceptedIssuers() = arrayOf<X509Certificate>()
                })
                val sslContext = SSLContext.getInstance("SSL")
                sslContext.init(null, trustAllCerts, java.security.SecureRandom())
                val sslSocketFactory = sslContext.socketFactory
                sslSocketFactory(sslSocketFactory, trustAllCerts[0] as X509TrustManager)
                hostnameVerifier { _, _ -> true }
            }
            .build()
    }

    fun testToken(): String? {
        return tokenManager?.let { manager ->
            runBlocking { manager.getAccessToken() }
        }
    }

    fun resetServerAvailable() {
        _serverAvailable.value = true
    }
}