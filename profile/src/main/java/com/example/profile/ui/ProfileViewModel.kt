package com.example.profile.ui

import android.app.Application
import android.net.Uri
import androidx.lifecycle.AndroidViewModel
import com.example.security.NameValidator
import com.example.security.ProfanityFilter
import androidx.lifecycle.viewModelScope
import com.example.core_models.ProfileState
import com.example.data.repository.ServerProfileRepositoryImpl
import com.example.data.repository.ServerAuthRepositoryImpl
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import android.util.Log

class ProfileViewModel(application: Application) : AndroidViewModel(application) {

    private val profileRepository = ServerProfileRepositoryImpl(application)
    private val authRepository = ServerAuthRepositoryImpl(application)
    private val profanityFilter = ProfanityFilter()

    private val _state = MutableStateFlow(ProfileState())
    val state: StateFlow<ProfileState> = _state.asStateFlow()

    init {
        loadAvailableAvatars()
        loadProfileIfAuthorized()
    }

    private fun loadProfileData() {

        viewModelScope.launch {
            _state.update { it.copy(isLoading = true, error = null) }

            val tokenManager = com.example.data.TokenManager(getApplication())
            val token = tokenManager.getAccessToken()
            val userData = tokenManager.getUserData()

            val cleanToken = token?.trim()?.removeSurrounding("\"")
            Log.d("PROFILE_VM", "🔑 3. Токен: ${token?.take(20)}...")
            Log.d("PROFILE_VM", "📦 4. UserData из хранилища: $userData")

            try {
                profileRepository.getProfileData().collect { profileState ->
                    _state.update {
                        profileState.copy(
                            availableAvatars = _state.value.availableAvatars,
                            isLoading = false
                        )
                    }
                }
            } catch (e: Exception) {
                _state.update { it.copy(error = e.message, isLoading = false) }
            }
        }
    }

    fun loadProfileIfAuthorized() {
        viewModelScope.launch {
            val tokenManager = com.example.data.TokenManager(getApplication())
            val token = tokenManager.getAccessToken()

            if (token != null) {
                loadProfileData()
            }
        }
    }

    private fun loadAvailableAvatars() {
        viewModelScope.launch {
            val result = profileRepository.getAvailableAvatars()
            result.onSuccess { urls ->
                _state.update { it.copy(availableAvatars = urls) }
            }.onFailure { error ->
                _state.update { it.copy(error = error.message) }
            }
        }
    }

    fun logout(onSuccess: () -> Unit) {
        viewModelScope.launch {
            _state.update { it.copy(isLoading = true) }

            authRepository.signOut()

            _state.update { it.copy(isLoading = false) }

            onSuccess()
        }
    }

    fun changeAvatar(uri: Uri) {
        _state.update { it.copy(error = "Загрузка аватаров не поддерживается. Выберите аватар из списка.") }
    }

    fun selectAvatarFromList(url: String) {
        _state.update { it.copy(avatarUrl = url, isLoading = true, error = null) }

        viewModelScope.launch {
            val result = profileRepository.updateUserAvatarUrl(url)
            result.onSuccess {
                _state.update { it.copy(isLoading = false) }
            }.onFailure { error ->
                _state.update { it.copy(error = error.message, isLoading = false) }
            }
        }
    }

    fun changeUserName(newName: String) {
        val trimmedName = newName.trim()

        if (trimmedName.isBlank()) {
            _state.update { it.copy(error = "Имя не может быть пустым") }
            return
        }

        if (profanityFilter.containsProfanity(trimmedName)) {
            _state.update { it.copy(error = "Имя содержит недопустимые слова") }
            return
        }

        val nameValidator = NameValidator()
        if (!nameValidator.isValid(trimmedName)) {
            _state.update { it.copy(error = "Имя должно содержать минимум 2 буквы, только буквы, цифры и знак _") }
            return
        }

        _state.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            val result = profileRepository.updateUserName(trimmedName)
            result.onSuccess {
                _state.update {
                    it.copy(
                        name = trimmedName,
                        isLoading = false
                    )
                }
            }.onFailure { error ->
                _state.update {
                    it.copy(
                        error = error.message ?: "Ошибка изменения имени",
                        isLoading = false
                    )
                }
            }
        }
    }

    fun updateElo(points: Int) {
        if (points < 0 || points > 10000) {
            _state.update { it.copy(error = "ELO должен быть от 0 до 10000") }
            return
        }

        _state.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            val result = profileRepository.updateEloPoints(points)

            if (result.isSuccess) {
                _state.update {
                    it.copy(
                        eloPoints = points,
                        isLoading = false
                    )
                }
            } else {
                _state.update {
                    it.copy(
                        error = result.exceptionOrNull()?.message ?: "Ошибка обновления ELO",
                        isLoading = false
                    )
                }
            }
        }
    }


    fun sendEmailChangeOtp(newEmail: String, onResult: (Boolean, String?) -> Unit) {
        if (newEmail.isBlank() || !android.util.Patterns.EMAIL_ADDRESS.matcher(newEmail).matches()) {
            onResult(false, "Введите корректный email")
            return
        }

        _state.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            val result = profileRepository.sendEmailChangeOtp(newEmail)
            _state.update { it.copy(isLoading = false) }

            if (result.isSuccess) {
                onResult(true, result.getOrNull())
            } else {
                onResult(false, result.exceptionOrNull()?.message)
            }
        }
    }

    fun verifyEmailChange(newEmail: String, code: String, onResult: (Boolean, String?) -> Unit) {
        if (code.length != 6 || !code.all { it.isDigit() }) {
            onResult(false, "Код должен содержать 6 цифр")
            return
        }

        _state.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            val result = profileRepository.verifyEmailChange(newEmail, code)
            _state.update { it.copy(isLoading = false) }

            if (result.isSuccess) {
                loadProfileData()
                onResult(true, "Email успешно изменен")
            } else {
                onResult(false, result.exceptionOrNull()?.message)
            }
        }
    }


    fun refreshProfile() {
        loadProfileData()
    }

    fun clearError() {
        _state.update { it.copy(error = null) }
    }

    fun clearNotification() {
        _state.update { it.copy(notificationMessage = null) }
    }
}