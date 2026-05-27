package com.example.learning

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.core_models.api.TaskApiService
import com.example.core_data.repository.ServerTaskRepositoryImpl
import com.example.data.AppConfig
import com.example.data.network.HttpClientFactory
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

class TaskViewModelFactory(
    private val context: Context
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(TaskViewModel::class.java)) {
            val retrofit = Retrofit.Builder()
                .baseUrl("${AppConfig.serverUrl}/")
                .client(HttpClientFactory.createUnsafeOkHttpClient())
                .addConverterFactory(GsonConverterFactory.create())
                .build()

            val apiService = retrofit.create(TaskApiService::class.java)
            val repository = ServerTaskRepositoryImpl(apiService)

            @Suppress("UNCHECKED_CAST")
            return TaskViewModel(repository) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class: ${modelClass.name}")
    }
}