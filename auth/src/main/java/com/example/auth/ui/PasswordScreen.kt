package com.example.auth.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusDirection
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.auth.domain.AuthViewModel
import com.example.security.PasswordValidator
import com.example.core_ui.PasswordStrengthIndicator
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun PasswordScreen(
    viewModel: AuthViewModel,
    passwordValidator: PasswordValidator = PasswordValidator(),
    onNext: () -> Unit,
    onBack: () -> Unit
) {
    val state by viewModel.uiState.collectAsState()

    var showPasswordMismatchError by remember { mutableStateOf(false) }
    var isPasswordVisible by remember { mutableStateOf(false) }
    var isRepeatVisible by remember { mutableStateOf(false) }
    var isCheckingPasswords by remember { mutableStateOf(false) }

    val focusManager = LocalFocusManager.current
    val keyboardController = LocalSoftwareKeyboardController.current
    val coroutineScope = rememberCoroutineScope()

    val validation = passwordValidator.getDetailedValidation(state.password)
    val strength = passwordValidator.calculateStrength(state.password)

    LaunchedEffect(state.password, state.passwordRepeat) {
        if (state.passwordRepeat.isNotEmpty()) {
            delay(1000)
            showPasswordMismatchError = state.password != state.passwordRepeat
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp)
            .imePadding(),
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            "Придумайте пароль",
            style = MaterialTheme.typography.headlineMedium
        )

        Spacer(modifier = Modifier.height(16.dp))

        OutlinedTextField(
            value = state.password,
            onValueChange = {
                viewModel.setPassword(it, state.passwordRepeat)
                showPasswordMismatchError = false
            },
            label = { Text("Пароль") },
            modifier = Modifier.fillMaxWidth(),
            visualTransformation = if (isPasswordVisible)
                VisualTransformation.None
            else
                PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Next
            ),
            keyboardActions = KeyboardActions(
                onNext = { focusManager.moveFocus(FocusDirection.Down) }
            ),
            trailingIcon = {
                IconButton(
                    onClick = {
                        isPasswordVisible = !isPasswordVisible
                    }
                ) {
                    Icon(
                        if (isPasswordVisible)
                            Icons.Default.Visibility
                        else
                            Icons.Default.VisibilityOff,
                        null
                    )
                }
            },
            supportingText = {
                Text(
                    text = "${state.password.length} / 25",
                    modifier = Modifier.fillMaxWidth(),
                    textAlign = TextAlign.End
                )
            },
            isError = state.errorMessage != null || showPasswordMismatchError,
            singleLine = true
        )

        PasswordStrengthIndicator(strength)

        validation.requirements.forEach { (text, met) ->
            Text(
                text = (if (met) "✓ " else "• ") + text,
                color = if (met) Color(0xFF4CAF50) else Color.Gray,
                style = MaterialTheme.typography.bodySmall,
                modifier = Modifier.padding(vertical = 2.dp)
            )
        }

        Spacer(modifier = Modifier.height(8.dp))

        OutlinedTextField(
            value = state.passwordRepeat,
            onValueChange = {
                viewModel.setPassword(state.password, it)
                coroutineScope.launch {
                    delay(500)
                    if (it.isNotEmpty() && state.password != it) {
                        showPasswordMismatchError = true
                    } else {
                        showPasswordMismatchError = false
                    }
                }
            },
            label = { Text("Повторите пароль") },
            modifier = Modifier.fillMaxWidth(),
            visualTransformation = if (isRepeatVisible)
                VisualTransformation.None
            else
                PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Done
            ),
            keyboardActions = KeyboardActions(
                onDone = {
                    if (!validation.isValid) {
                        viewModel.setError("Пароль не соответствует требованиям")
                        return@KeyboardActions
                    }
                    if (state.password != state.passwordRepeat) {
                        showPasswordMismatchError = true
                        return@KeyboardActions
                    }

                    keyboardController?.hide()
                    focusManager.clearFocus()
                    onNext()
                }
            ),
            trailingIcon = {
                IconButton(
                    onClick = {
                        isRepeatVisible = !isRepeatVisible
                    }
                ) {
                    Icon(
                        if (isRepeatVisible)
                            Icons.Default.Visibility
                        else
                            Icons.Default.VisibilityOff,
                        null
                    )
                }
            },
            supportingText = {
                Column {
                    Text(
                        text = "${state.passwordRepeat.length} / 25",
                        modifier = Modifier.fillMaxWidth(),
                        textAlign = TextAlign.End
                    )
                    if (showPasswordMismatchError) {
                        Text(
                            text = "Пароли не совпадают",
                            color = MaterialTheme.colorScheme.error,
                            modifier = Modifier.fillMaxWidth(),
                            textAlign = TextAlign.Start
                        )
                    }
                }
            },
            isError = state.errorMessage != null || showPasswordMismatchError,
            singleLine = true
        )

        state.errorMessage?.let {
            Text(
                it,
                color = MaterialTheme.colorScheme.error,
                modifier = Modifier.padding(top = 8.dp)
            )
        }

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = {
                keyboardController?.hide()
                focusManager.clearFocus()

                if (!validation.isValid) {
                    viewModel.setError("Пароль не соответствует требованиям")
                    return@Button
                }
                if (state.password != state.passwordRepeat) {
                    coroutineScope.launch {
                        isCheckingPasswords = true
                        delay(300)
                        showPasswordMismatchError = true
                        isCheckingPasswords = false
                    }
                    return@Button
                }
                onNext()
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            if (isCheckingPasswords) {
                CircularProgressIndicator(
                    modifier = Modifier.size(20.dp),
                    strokeWidth = 2.dp
                )
            } else {
                Text("Далее")
            }
        }

        TextButton(
            onClick = {
                keyboardController?.hide()
                focusManager.clearFocus()
                viewModel.resetAuthProgress()
                onBack()
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Назад")
        }
    }
}