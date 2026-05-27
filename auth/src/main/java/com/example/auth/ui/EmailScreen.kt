package com.example.auth.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.example.auth.domain.AuthViewModel
import com.example.security.EmailValidator
import com.example.security.YandexEmailValidator

@Composable
fun EmailScreen(
    viewModel: AuthViewModel,
    onNext: () -> Unit,
    onBack: () -> Unit,
    emailValidator: EmailValidator = YandexEmailValidator()
) {
    val state by viewModel.uiState.collectAsState()
    val focusManager = LocalFocusManager.current
    val emailCharacterLimit = 35

    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
        ) {
            IconButton(onClick = onBack) {
                Icon(Icons.Default.ArrowBack, contentDescription = "Назад")
            }
            Text(
                text = "Введите почту",
                style = MaterialTheme.typography.headlineMedium,
                modifier = Modifier.padding(start = 8.dp)
            )
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            OutlinedTextField(
                value = state.email,
                onValueChange = { input ->
                    if (input.length <= emailCharacterLimit) {
                        viewModel.setEmail(input)
                    }
                },
                label = { Text("Email") },
                placeholder = { Text("example@yandex.ru") },
                modifier = Modifier.fillMaxWidth(),
                keyboardOptions = KeyboardOptions(
                    keyboardType = KeyboardType.Email,
                    imeAction = ImeAction.Done
                ),
                keyboardActions = KeyboardActions(onDone = { focusManager.clearFocus() }),
                isError = state.errorMessage != null,
                singleLine = true,
                supportingText = {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        state.errorMessage?.let {
                            Text(
                                text = it,
                                color = MaterialTheme.colorScheme.error
                            )
                        }
                        Text(
                            text = "${state.email.length} / $emailCharacterLimit",
                            textAlign = androidx.compose.ui.text.style.TextAlign.End
                        )
                    }
                }
            )

            Spacer(modifier = Modifier.weight(1f))

            Button(
                onClick = {
                    focusManager.clearFocus()
                    if (emailValidator.isValid(state.email)) {
                        if (state.otpCode.isNotEmpty()) onNext()
                        else viewModel.sendCode(state.email, onSuccess = onNext)
                    } else {
                        viewModel.setError("Используйте почту: yandex.ru, ya.ru, mail.ru, bk.ru, list.ru, inbox.ru, rambler.ru, vk.com и другие")
                    }
                },
                enabled = !state.isLoading && state.email.isNotEmpty(),
                modifier = Modifier.fillMaxWidth()
            ) {
                if (state.isLoading) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(24.dp),
                        strokeWidth = 2.dp
                    )
                } else {
                    Text("Далее")
                }
            }
        }
    }
}