package com.example.auth.domain

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.example.auth.domain.usecase.RegisterUserUseCase
import com.example.auth.domain.usecase.SendOtpUseCase
import com.example.core_models.AuthState
import com.example.core_models.AuthStep
import com.example.core_models.repository.AuthRepository
import com.example.data.repository.ServerAuthRepositoryImpl
import com.example.security.NameValidator
import com.example.security.ProfanityFilter
import com.example.security.UsernameGenerator
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class AuthViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(AuthState())
    val uiState: StateFlow<AuthState> = _uiState.asStateFlow()

    private val repository: AuthRepository = ServerAuthRepositoryImpl(application)
    private val registerUserUseCase = RegisterUserUseCase(repository)
    private val sendOtpUseCase = SendOtpUseCase(repository)

    private val _emailInput = MutableSharedFlow<String>()
    private val nameValidator = NameValidator()
    private val profanityFilter = ProfanityFilter()

    init {
        checkInitialAuth()
        setupEmailDebounce()
    }

    fun resetAllRegistrationData() {
        _uiState.update {
            it.copy(
                email = "",
                userName = "",
                selectedAvatarUrl = null,
                otpCode = "",
                userEnteredOtp = "",
                password = "",
                passwordRepeat = "",
                suggestedNames = emptyList(),
                availableAvatars = emptyList(),
                errorMessage = null
            )
        }
    }

    fun resetToInitialState() {
        resetAllRegistrationData()
        updateStep(AuthStep.EMAIL)
    }

    private fun loadAvailableAvatars() {
        viewModelScope.launch {
            repository.getAvailableAvatars().onSuccess { urls ->
                _uiState.update {
                    it.copy(
                        availableAvatars = urls,
                        selectedAvatarUrl = urls.firstOrNull(),
                        errorMessage = null
                    )
                }
            }.onFailure { e ->
                setError("Ошибка загрузки аватаров: ${e.message}")
            }
        }
    }

    private fun loadUsernameSuggestions() {
        val suggestions = List(3) { UsernameGenerator.generate() }
        _uiState.update { it.copy(suggestedNames = suggestions) }
    }

    fun updateStep(step: AuthStep) {
        _uiState.update { it.copy(step = step, errorMessage = null) }

        viewModelScope.launch {
            repository.saveAuthStep(step)
            if (step == AuthStep.NAME_AVATAR) {
                loadAvailableAvatars()
                loadUsernameSuggestions()
            }
        }
    }

    fun clearError() {
        _uiState.update { it.copy(errorMessage = null) }
    }

    fun selectAvatar(url: String) {
        _uiState.update { it.copy(selectedAvatarUrl = url) }
    }

    private fun checkInitialAuth() {
        viewModelScope.launch {
            if (repository.isUserLoggedIn()) {
                _uiState.update { it.copy(isUserAuthenticated = true) }
            }
            val savedStep = repository.getSavedAuthStep()
            _uiState.update { it.copy(step = savedStep) }
        }
    }

    @OptIn(FlowPreview::class)
    private fun setupEmailDebounce() {
        viewModelScope.launch {
            _emailInput.debounce(4000L).distinctUntilChanged().collect { email ->
                if (email.isNotEmpty() && !android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
                    setError("Некорректный формат почты")
                } else {
                    setError(null)
                }
            }
        }
    }

    fun setEmail(email: String) {
        val cleanEmail = email.replace(" ", "")
        if (_uiState.value.email != cleanEmail) {
            _uiState.update { it.copy(email = cleanEmail, otpCode = "", userEnteredOtp = "", errorMessage = null) }
        }
        viewModelScope.launch { _emailInput.emit(cleanEmail) }
    }

    fun setOtpCode(code: String) {
        val cleanCode = code.replace(" ", "")
        _uiState.update { it.copy(userEnteredOtp = cleanCode, errorMessage = null) }
    }

    fun setPassword(password: String, repeat: String) {
        val cleanPassword = password.replace(" ", "")
        val cleanRepeat = repeat.replace(" ", "")

        if (cleanPassword.length <= 25 && cleanRepeat.length <= 25) {
            _uiState.update {
                it.copy(
                    password = cleanPassword,
                    passwordRepeat = cleanRepeat,
                    errorMessage = null
                )
            }
        }
    }

    fun setError(message: String?) {
        _uiState.update { it.copy(errorMessage = message) }
    }

    fun setUserName(name: String) {
        if (name.startsWith(" ")) return
        if (name.contains("  ")) return

        val spaceCount = name.count { it == ' ' }
        if (name.endsWith(" ") && spaceCount > 1) return

        if (name.length <= 30) {
            _uiState.update { it.copy(userName = name, errorMessage = null) }
        }
    }

    fun sendCode(email: String, onSuccess: () -> Unit) {
        _uiState.update { it.copy(isLoading = true, otpCode = "", userEnteredOtp = "") }
        viewModelScope.launch {
            val result = sendOtpUseCase.execute(email)
            _uiState.update { it.copy(isLoading = false) }
            result.onSuccess { message ->
                _uiState.update { it.copy(otpCode = "SENT") }
                onSuccess()
            }.onFailure { error ->
                setError(error.message ?: "Ошибка отправки кода")
            }
        }
    }

    fun verifyCode(onSuccess: () -> Unit) {
        val enteredCode = _uiState.value.userEnteredOtp
        if (enteredCode.length != 6) {
            setError("Код должен содержать 6 цифр")
            return
        }

        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            val result = repository.verifyOtp(
                email = _uiState.value.email,
                code = enteredCode
            )

            _uiState.update { it.copy(isLoading = false) }

            result.onSuccess {
                onSuccess()
            }.onFailure { error ->
                setError(error.message ?: "Неверный код")
            }
        }
    }

    fun registerUser(onSuccess: () -> Unit) {
        _uiState.update { it.copy(isLoading = true) }
        viewModelScope.launch {
            val authResult = registerUserUseCase.execute(_uiState.value)

            authResult
                .onSuccess {
                    _uiState.update {
                        it.copy(
                            isLoading = false,
                            isUserAuthenticated = true
                        )
                    }
                    onSuccess()
                }
                .onFailure { error ->
                    _uiState.update { it.copy(isLoading = false) }
                    setError("Ошибка регистрации: ${error.message}")
                }
        }
    }

    fun finishRegistration(onSuccess: () -> Unit) {
        val currentName = _uiState.value.userName.trim()
        val avatarUrl = _uiState.value.selectedAvatarUrl

        setError(null)

        if (currentName.isEmpty()) {
            setError("Введите имя")
            return
        }

        if (currentName.length < 2) {
            setError("Имя должно содержать минимум 2 символа")
            return
        }

        if (currentName.length > 30) {
            setError("Имя должно быть не длиннее 30 символов")
            return
        }

        if (!nameValidator.isValid(currentName)) {
            setError("Имя должно содержать буквы. Разрешены буквы, цифры и _")
            return
        }

        if (profanityFilter.containsProfanity(currentName)) {
            setError("Имя содержит недопустимые слова")
            return
        }

        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            try {
                val checkResult = repository.isUsernameUnique(currentName)

                if (checkResult.isSuccess) {
                    val isUnique = checkResult.getOrNull()

                    if (isUnique == true) {
                        val registrationResult = registerUserUseCase.execute(_uiState.value)

                        if (registrationResult.isSuccess) {
                            _uiState.update {
                                it.copy(
                                    isLoading = false,
                                    isUserAuthenticated = true
                                )
                            }
                            onSuccess()
                        }
                    }
                }
            } catch (e: Exception) {
            }
        }
    }

    private fun handleUsernameCheckError(error: Throwable?) {
        when (error) {
            is retrofit2.HttpException -> {
                when (error.code()) {
                    401 -> {
                        setError("Ошибка доступа к серверу (401)")
                    }
                    400 -> setError("Некорректное имя. Проверьте формат")
                    409 -> setError("Это имя уже занято")
                    404 -> setError("Сервер недоступен. Проверьте подключение")
                    else -> setError("Ошибка сервера (${error.code()})")
                }
            }
            is java.net.ConnectException -> {
                setError("Нет подключения к серверу")
            }
            is java.net.SocketTimeoutException -> {
                setError("Превышено время ожидания ответа от сервера")
            }
            else -> setError("Ошибка: ${error?.message ?: "Неизвестная ошибка"}")
        }
    }
    fun isUserNameValid(): Boolean {
        val name = _uiState.value.userName.trim()
        return nameValidator.isValid(name)
    }

    fun resetAuthProgress() {
        _uiState.update {
            it.copy(
                otpCode = "",
                userEnteredOtp = "",
                password = "",
                passwordRepeat = "",
                errorMessage = null
            )
        }
    }

    fun resetNameAndAvatar() {
        _uiState.update {
            it.copy(
                userName = "",
                selectedAvatarUrl = null,
                suggestedNames = emptyList(),
                availableAvatars = emptyList()
            )
        }
    }

    fun login(email: String, password: String, onResult: (Result<Unit>) -> Unit) {
        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            val result = repository.login(email, password)
            _uiState.update { it.copy(isLoading = false) }

            if (result.isSuccess) {
                _uiState.update { it.copy(isUserAuthenticated = true) }
            }
            onResult(result)
        }
    }

    fun forgotPassword(email: String, onResult: (Boolean, String?) -> Unit) {
        if (email.isEmpty() || !android.util.Patterns.EMAIL_ADDRESS.matcher(email).matches()) {
            onResult(false, "Введите корректный email")
            return
        }

        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            val result = repository.forgotPassword(email)
            _uiState.update { it.copy(isLoading = false) }

            if (result.isSuccess) {
                val message = result.getOrNull()
                onResult(true, message ?: "Инструкция отправлена на email")
            } else {
                val error = result.exceptionOrNull()?.message ?: "Неизвестная ошибка"
                onResult(false, error)
            }
        }
    }

    fun changeEmail(newEmail: String, onResult: (Boolean, String?) -> Unit) {
        if (newEmail.isEmpty() || !android.util.Patterns.EMAIL_ADDRESS.matcher(newEmail).matches()) {
            onResult(false, "Введите корректный email")
            return
        }

        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            val result = repository.changeEmail(newEmail)
            _uiState.update { it.copy(isLoading = false) }

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

        _uiState.update { it.copy(isLoading = true) }

        viewModelScope.launch {
            val result = repository.verifyEmailChange(newEmail, code)
            _uiState.update { it.copy(isLoading = false) }

            if (result.isSuccess) {
                refreshUserData()
                onResult(true, "Email успешно изменен")
            } else {
                onResult(false, result.exceptionOrNull()?.message)
            }
        }
    }

    private suspend fun refreshUserData() {
        val userData = repository.getCurrentUser()
        userData?.let {
            _uiState.update { state ->
                state.copy(currentUser = userData)
            }
        }
    }

    fun testConnection(onResult: (Boolean, String?) -> Unit) {
        viewModelScope.launch {
            try {
                val result = repository.testConnection()
                result.onSuccess { data ->
                    val users = data["totalUsers"] ?: 0
                    onResult(true, "Сервер доступен. Пользователей: $users")
                }.onFailure { error ->
                    onResult(false, "Ошибка: ${error.message}")
                }
            } catch (e: Exception) {
                onResult(false, "Ошибка подключения: ${e.message}")
            }
        }
    }
}