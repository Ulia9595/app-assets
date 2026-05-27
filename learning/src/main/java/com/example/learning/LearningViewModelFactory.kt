package com.example.learning

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.core_data.repository.ServerLearningRepositoryImpl
import com.example.core_models.api.ServerLearningApiService
import com.example.data.AppConfig
import com.example.data.network.HttpClientFactory
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class LearningViewModelFactory(
    private val context: Context
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(LearningViewModel::class.java)) {
            val okHttpClient = HttpClientFactory.createUnsafeOkHttpClient()

            val retrofit = Retrofit.Builder()
                .baseUrl("${AppConfig.serverUrl}/")
                .client(okHttpClient)
                .addConverterFactory(GsonConverterFactory.create())
                .build()

            val apiService = retrofit.create(ServerLearningApiService::class.java)
            val repository = ServerLearningRepositoryImpl(apiService)

            @Suppress("UNCHECKED_CAST")
            return LearningViewModel(repository) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}