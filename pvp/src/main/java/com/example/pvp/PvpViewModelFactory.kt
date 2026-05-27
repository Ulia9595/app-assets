package com.example.pvp

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.core_data.pvp.PvpManager

class PvpViewModelFactory(
    private val pvpManager: PvpManager
) : ViewModelProvider.Factory {
    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        require(modelClass == PvpViewModel::class.java)
        return PvpViewModel(pvpManager) as T
    }
}