package com.example.auth.ui

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.example.auth.domain.AuthViewModel

@Composable
fun CodeScreen(
    viewModel: AuthViewModel,
    onNext: () -> Unit,
    onBack: () -> Unit
) {
    val state by viewModel.uiState.collectAsState()
    val focusManager = LocalFocusManager.current

    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text("Подтверждение почты", style = MaterialTheme.typography.headlineMedium)
        Text("Введите код для ${state.email}", style = MaterialTheme.typography.bodySmall)

        Spacer(modifier = Modifier.height(16.dp))

        OutlinedTextField(
            value = state.userEnteredOtp,
            onValueChange = { if (it.length <= 6) viewModel.setOtpCode(it) },
            label = { Text("Код") },
            modifier = Modifier.fillMaxWidth(),
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Number,
                imeAction = ImeAction.Done
            ),
            keyboardActions = KeyboardActions(onDone = { focusManager.clearFocus() }),
            isError = state.errorMessage != null,
            singleLine = true
        )

        state.errorMessage?.let {
            Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
        }

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = {
                focusManager.clearFocus()
                viewModel.verifyCode(onSuccess = onNext)
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Подтвердить")
        }

        TextButton(
            onClick = {
                viewModel.resetAuthProgress()
                onBack()
            },
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Назад")
        }
    }
}