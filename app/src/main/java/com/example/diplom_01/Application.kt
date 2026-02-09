package com.example.diplom_01

import android.app.Application
import com.example.data.network.HttpClientFactory

class App : Application() {

    override fun onCreate() {
        super.onCreate()
        HttpClientFactory.initialize(this)
    }
}
