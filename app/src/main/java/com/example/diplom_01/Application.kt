package com.example.diplom_01

import android.app.Application
import com.example.data.AppConfig
import com.example.data.network.HttpClientFactory
import com.example.diplom_01.BuildConfig

class App : Application() {

    override fun onCreate() {
        super.onCreate()
        HttpClientFactory.initialize(this)
        AppConfig.serverUrl = BuildConfig.SERVER_URL
    }
}