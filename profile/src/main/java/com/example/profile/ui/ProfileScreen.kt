import android.util.Log
import android.util.Patterns
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalSoftwareKeyboardController
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import coil.compose.AsyncImage
import com.example.profile.ui.ProfileViewModel
import com.example.security.NameValidator
import kotlinx.coroutines.delay
import com.example.security.YandexEmailValidator

@Composable
fun ProfileScreen(
    profileViewModel: ProfileViewModel = viewModel(),
    onLogoutNavigate: () -> Unit
) {
    val state by profileViewModel.state.collectAsState()

    var isEditingName by remember { mutableStateOf(false) }
    var tempName by remember(state.name) { mutableStateOf(state.name) }
    var showAvatarSelection by remember { mutableStateOf(false) }
    var showChangeEmailDialog by remember { mutableStateOf(false) }
    var showOtpDialog by remember { mutableStateOf(false) }
    var tempNewEmail by remember { mutableStateOf("") }
    var otpCode by remember { mutableStateOf("") }

    val nameValidator = remember { NameValidator() }
    val profanityFilter = remember { com.example.security.ProfanityFilter() }
    val isNameValid = remember(tempName) {
        nameValidator.isValid(tempName) && !profanityFilter.containsProfanity(tempName)
    }

    state.notificationMessage?.let { message ->
        AlertDialog(
            onDismissRequest = { profileViewModel.clearNotification() },
            title = { Text("Уведомление") },
            text = { Text(message) },
            confirmButton = {
                TextButton(onClick = { profileViewModel.clearNotification() }) { Text("OK") }
            }
        )
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        if (state.isLoading) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else {
            Box(contentAlignment = Alignment.BottomEnd) {
                AsyncImage(
                    model = state.avatarUrl ?: "https://via.placeholder.com/150",
                    contentDescription = "Avatar",
                    modifier = Modifier
                        .size(140.dp)
                        .aspectRatio(1f)
                        .clip(CircleShape)
                        .border(2.dp, MaterialTheme.colorScheme.outlineVariant, CircleShape),
                    contentScale = ContentScale.Crop
                )

                Surface(
                    shape = CircleShape,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier
                        .size(40.dp)
                        .offset(x = 4.dp, y = 4.dp)
                        .clickable { showAvatarSelection = true }
                        .border(2.dp, MaterialTheme.colorScheme.surface, CircleShape),
                    tonalElevation = 4.dp
                ) {
                    Icon(
                        imageVector = Icons.Default.Edit,
                        contentDescription = "Edit Avatar",
                        tint = Color.White,
                        modifier = Modifier.padding(10.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Text(
                text = maskEmail(state.email),
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.padding(bottom = 4.dp)
            )

            TextButton(
                onClick = { showChangeEmailDialog = true },
                modifier = Modifier.padding(bottom = 8.dp)
            ) {
                Icon(
                    Icons.Default.Email,
                    contentDescription = null,
                    modifier = Modifier.size(16.dp)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text(
                    "Сменить email",
                    style = MaterialTheme.typography.bodySmall
                )
            }

            if (isEditingName) {
                OutlinedTextField(
                    value = tempName,
                    onValueChange = {
                        val newValue = it.take(30)
                        tempName = newValue
                    },
                    label = { Text("Имя пользователя") },
                    singleLine = true,
                    isError = !isNameValid && tempName.isNotEmpty(),
                    supportingText = {
                        Column(modifier = Modifier.fillMaxWidth()) {
                            Text(
                                text = "${tempName.length} / 30",
                                style = MaterialTheme.typography.labelSmall,
                                modifier = Modifier.fillMaxWidth(),
                                textAlign = androidx.compose.ui.text.style.TextAlign.End
                            )

                            if (!isNameValid && tempName.isNotEmpty()) {
                                if (profanityFilter.containsProfanity(tempName)) {
                                    Text(
                                        text = "Имя содержит недопустимые слова",
                                        color = MaterialTheme.colorScheme.error,
                                        style = MaterialTheme.typography.bodySmall
                                    )
                                } else {
                                    Text(
                                        text = "Имя должно содержать минимум 2 буквы, только буквы, цифры и _",
                                        color = MaterialTheme.colorScheme.error,
                                        style = MaterialTheme.typography.bodySmall
                                    )
                                }
                            }
                        }
                    },
                    modifier = Modifier.fillMaxWidth(),
                    trailingIcon = {
                        TextButton(
                            onClick = {
                                if (isNameValid) {
                                    profileViewModel.changeUserName(tempName)
                                    isEditingName = false
                                }
                            },
                            enabled = isNameValid
                        ) {
                            Text(
                                "ОК",
                                fontWeight = FontWeight.Bold,
                                color = if (isNameValid) MaterialTheme.colorScheme.primary else Color.Gray
                            )
                        }
                    }
                )
            } else {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    modifier = Modifier
                        .clip(RoundedCornerShape(8.dp))
                        .clickable {
                            tempName = state.name
                            isEditingName = true
                        }
                        .padding(8.dp)
                ) {
                    Text(
                        text = state.name.ifEmpty { "Нажмите, чтобы задать имя" },
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(modifier = Modifier.width(8.dp))
                    Icon(
                        Icons.Default.Edit,
                        contentDescription = null,
                        modifier = Modifier.size(18.dp)
                    )
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(16.dp),
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(
                        alpha = 0.5f
                    )
                )
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.Bottom
                    ) {
                        Column {
                            Text(
                                text = "Уровень ${state.level}",
                                style = MaterialTheme.typography.titleMedium,
                                color = MaterialTheme.colorScheme.primary,
                                fontWeight = FontWeight.Bold
                            )
                            Text(
                                text = "Всего: ${state.eloPoints} ELO",
                                style = MaterialTheme.typography.labelMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                        }
                        Text(
                            text = "${state.pointsInCurrentLevel}/1000",
                            style = MaterialTheme.typography.titleSmall,
                            fontWeight = FontWeight.SemiBold
                        )
                    }

                    Spacer(modifier = Modifier.height(12.dp))

                    LinearProgressIndicator(
                        progress = { state.levelProgress.coerceIn(0f, 1f) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(10.dp)
                            .clip(CircleShape),
                        color = MaterialTheme.colorScheme.primary,
                        trackColor = MaterialTheme.colorScheme.outlineVariant
                    )

                    Text(
                        text = "До уровня ${state.level + 1} осталось ${state.pointsToNextLevel} очков",
                        style = MaterialTheme.typography.labelSmall,
                        modifier = Modifier.padding(top = 8.dp),
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(top = 16.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                Button(
                    onClick = {
                        val newElo = state.eloPoints + 100
                        profileViewModel.updateElo(newElo)
                    },
                    modifier = Modifier.weight(1f),
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.tertiaryContainer,
                        contentColor = MaterialTheme.colorScheme.onTertiaryContainer
                    )
                ) {
                    Text("+100 ELO")
                }

                OutlinedButton(
                    onClick = { profileViewModel.refreshProfile() },
                    modifier = Modifier.weight(1f)
                ) {
                    Text("Обновить")
                }
            }

            if (state.error != null) {
                Text(
                    text = state.error!!,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier
                        .padding(top = 16.dp)
                        .clickable { profileViewModel.clearError() }
                )
            }

            Spacer(modifier = Modifier.weight(1f))

            Button(
                onClick = { profileViewModel.logout { onLogoutNavigate() } },
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(
                    containerColor = Color(0xFFFFCDD2),
                    contentColor = Color(0xFFD32F2F)
                )
            ) {
                Text("Выйти из аккаунта")
            }
        }
    }

    if (showAvatarSelection) {
        AlertDialog(
            onDismissRequest = { showAvatarSelection = false },
            title = { Text("Выберите аватар") },
            text = {
                Box(modifier = Modifier.heightIn(max = 400.dp)) {
                    LazyVerticalGrid(
                        columns = GridCells.Fixed(3),
                        contentPadding = PaddingValues(8.dp),
                        horizontalArrangement = Arrangement.spacedBy(16.dp),
                        verticalArrangement = Arrangement.spacedBy(16.dp)
                    ) {
                        items(state.availableAvatars) { url ->
                            Box(
                                modifier = Modifier
                                    .aspectRatio(1f)
                                    .clip(CircleShape)
                                    .border(
                                        width = if (state.avatarUrl == url) 3.dp else 1.dp,
                                        color = if (state.avatarUrl == url) MaterialTheme.colorScheme.primary else Color.LightGray,
                                        shape = CircleShape
                                    )
                                    .clickable {
                                        profileViewModel.selectAvatarFromList(url)
                                        showAvatarSelection = false
                                    }
                            ) {
                                AsyncImage(
                                    model = url,
                                    contentDescription = null,
                                    modifier = Modifier.fillMaxSize(),
                                    contentScale = ContentScale.Crop
                                )
                            }
                        }
                    }
                }
            },
            confirmButton = {
                TextButton(onClick = { showAvatarSelection = false }) { Text("Отмена") }
            }
        )
    }

    if (showChangeEmailDialog) {
        var newEmail by remember { mutableStateOf("") }
        var emailError by remember { mutableStateOf<String?>(null) }
        val focusRequester = remember { FocusRequester() }
        val keyboardController = LocalSoftwareKeyboardController.current
        val yandexValidator = remember { YandexEmailValidator() }

        AlertDialog(
            onDismissRequest = {
                showChangeEmailDialog = false
                keyboardController?.hide()
            },
            title = { Text("Смена email") },
            text = {
                Column {
                    Text("Введите новый Yandex email для вашего аккаунта")
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = newEmail,
                        onValueChange = { input ->
                            val cleanEmail = input.filterNot { it.isWhitespace() }
                            newEmail = cleanEmail
                            emailError = null
                        },
                        label = { Text("Новый email") },
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Email,
                            imeAction = ImeAction.Done
                        ),
                        keyboardActions = KeyboardActions(
                            onDone = {
                                keyboardController?.hide()
                                val trimmedEmail = newEmail.trim()
                                when {
                                    trimmedEmail.isEmpty() -> {
                                        emailError = "Введите email"
                                    }
                                    !Patterns.EMAIL_ADDRESS.matcher(trimmedEmail).matches() -> {
                                        emailError = "Введите корректный email"
                                    }
                                    !yandexValidator.isValid(trimmedEmail) -> {
                                        emailError = "Используйте только @yandex.ru или @ya.ru"
                                    }
                                }
                            }
                        ),
                        isError = emailError != null,
                        modifier = Modifier
                            .fillMaxWidth()
                            .focusRequester(focusRequester),
                        supportingText = {
                            Column(modifier = Modifier.fillMaxWidth()) {
                                Row(
                                    horizontalArrangement = Arrangement.End,
                                    modifier = Modifier.fillMaxWidth()
                                ) {
                                    Text(
                                        text = "Только @yandex.ru / @ya.ru",
                                        color = if (emailError?.contains("yandex") == true)
                                            MaterialTheme.colorScheme.error
                                        else
                                            MaterialTheme.colorScheme.secondary,
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                }

                                if (emailError != null) {
                                    Spacer(modifier = Modifier.height(4.dp))
                                    Text(
                                        text = emailError!!,
                                        color = MaterialTheme.colorScheme.error,
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                }
                            }
                        }
                    )

                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "Пример: user@yandex.ru или user@ya.ru",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.secondary
                    )
                }
            },
            confirmButton = {
                Row(
                    horizontalArrangement = Arrangement.End,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    TextButton(
                        onClick = {
                            showChangeEmailDialog = false
                            keyboardController?.hide()
                        }
                    ) {
                        Text("Отмена")
                    }

                    Spacer(modifier = Modifier.width(8.dp))

                    Button(
                        onClick = {
                            val trimmedEmail = newEmail.trim()

                            when {
                                trimmedEmail.isEmpty() -> {
                                    emailError = "Введите email"
                                    return@Button
                                }

                                !Patterns.EMAIL_ADDRESS.matcher(trimmedEmail).matches() -> {
                                    emailError = "Введите корректный email"
                                    return@Button
                                }

                                !yandexValidator.isValid(trimmedEmail) -> {
                                    emailError = "Используйте только @yandex.ru или @ya.ru"
                                    return@Button
                                }
                            }

                            keyboardController?.hide()

                            profileViewModel.sendEmailChangeOtp(trimmedEmail) { success, message ->
                                if (success) {
                                    tempNewEmail = trimmedEmail
                                    showChangeEmailDialog = false
                                    showOtpDialog = true
                                } else {
                                    emailError = when {
                                        message?.contains("уже используется", ignoreCase = true) == true ->
                                            "Этот email уже используется"
                                        message?.contains("не найден", ignoreCase = true) == true ->
                                            "Пользователь не найден"
                                        message?.contains("network", ignoreCase = true) == true ->
                                            "Проблемы с интернет-соединением"
                                        message?.contains("не разрешено", ignoreCase = true) == true ->
                                            "Смена email временно недоступна"
                                        else -> message ?: "Ошибка отправки кода"
                                    }
                                }
                            }
                        },
                        modifier = Modifier.height(40.dp),
                        enabled = newEmail.trim().isNotEmpty()
                    ) {
                        Text("Отправить код")
                    }
                }
            },
            dismissButton = {}
        )

        LaunchedEffect(Unit) {
            delay(100)
            focusRequester.requestFocus()
        }
    }

    if (showOtpDialog) {
        var otpCode by remember { mutableStateOf("") }
        var otpError by remember { mutableStateOf<String?>(null) }
        val focusRequester = remember { FocusRequester() }
        val keyboardController = LocalSoftwareKeyboardController.current

        AlertDialog(
            onDismissRequest = {
                showOtpDialog = false
                otpCode = ""
                otpError = null
                keyboardController?.hide()
            },
            title = { Text("Подтверждение смены email") },
            text = {
                Column {
                    Text("Введите 6-значный код, отправленный на $tempNewEmail")
                    Spacer(modifier = Modifier.height(16.dp))
                    OutlinedTextField(
                        value = otpCode,
                        onValueChange = { input ->
                            val newText = input.filter { it.isDigit() }.take(6)
                            otpCode = newText
                            otpError = null

                            if (newText.length == 6) {
                                keyboardController?.hide()
                            }
                        },
                        label = { Text("Код") },
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Number,
                            imeAction = ImeAction.Done
                        ),
                        keyboardActions = KeyboardActions(
                            onDone = {
                                keyboardController?.hide()
                                if (otpCode.isEmpty()) {
                                    otpError = "Введите код"
                                } else if (otpCode.length < 6) {
                                    otpError = "Введите все 6 цифр"
                                }
                            }
                        ),
                        isError = otpError != null,
                        modifier = Modifier
                            .fillMaxWidth()
                            .focusRequester(focusRequester),
                        supportingText = {
                            Column(modifier = Modifier.fillMaxWidth()) {
                                Row(
                                    horizontalArrangement = Arrangement.End,
                                    modifier = Modifier.fillMaxWidth()
                                ) {
                                    Text(
                                        text = "${otpCode.length} / 6",
                                        color = if (otpCode.length == 6)
                                            MaterialTheme.colorScheme.primary
                                        else if (otpError != null)
                                            MaterialTheme.colorScheme.error
                                        else
                                            MaterialTheme.colorScheme.secondary,
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                }

                                if (otpError != null) {
                                    Spacer(modifier = Modifier.height(4.dp))
                                    Text(
                                        text = otpError!!,
                                        color = MaterialTheme.colorScheme.error,
                                        style = MaterialTheme.typography.labelSmall
                                    )
                                }
                            }
                        }
                    )
                }
            },
            confirmButton = {
                Row(
                    horizontalArrangement = Arrangement.End,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    // Кнопка Отмена
                    TextButton(
                        onClick = {
                            showOtpDialog = false
                            otpCode = ""
                            otpError = null
                            keyboardController?.hide()
                        }
                    ) {
                        Text("Отмена")
                    }

                    Spacer(modifier = Modifier.width(8.dp))

                    // Кнопка Подтвердить (текстовая)
                    Button(
                        onClick = {
                            // Валидация на клиенте
                            when {
                                otpCode.isEmpty() -> {
                                    otpError = "Введите код"
                                    return@Button
                                }

                                otpCode.length < 6 -> {
                                    otpError = "Введите все 6 цифр"
                                    return@Button
                                }

                                !otpCode.all { it.isDigit() } -> {
                                    otpError = "Код должен содержать только цифры"
                                    return@Button
                                }
                            }

                            keyboardController?.hide()

                            profileViewModel.verifyEmailChange(
                                tempNewEmail,
                                otpCode
                            ) { success, message ->
                                if (success) {
                                    // Успех: закрываем диалог
                                    showOtpDialog = false
                                    otpCode = ""
                                    otpError = null

                                    // Показываем уведомление об успехе
                                    // Можно использовать Snackbar или Toast
                                } else {
                                    // Ошибка: показываем сообщение от сервера
                                    otpError = when {
                                        message?.contains(
                                            "неверный",
                                            ignoreCase = true
                                        ) == true -> "Неверный код"

                                        message?.contains(
                                            "истёк",
                                            ignoreCase = true
                                        ) == true -> "Код устарел. Запросите новый"

                                        message?.contains("404") == true -> "Пользователь не найден"
                                        message?.contains("401") == true -> "Ошибка авторизации"
                                        message?.contains(
                                            "network",
                                            ignoreCase = true
                                        ) == true -> "Проблемы с интернет-соединением"

                                        else -> message ?: "Ошибка подтверждения кода"
                                    }
                                }
                            }
                        },
                        enabled = otpCode.length == 6,
                        modifier = Modifier.height(40.dp)
                    ) {
                        Text("Подтвердить")
                    }
                }
            },
            dismissButton = {}
        )

        // Автофокус при открытии диалога
        LaunchedEffect(showOtpDialog) {
            if (showOtpDialog) {
                delay(100)
                focusRequester.requestFocus()
            }
        }
    }
}

private fun maskEmail(email: String): String {
    if (email.isBlank() || !email.contains("@")) return email
    val parts = email.split("@")
    val localPart = parts[0]
    val domain = parts[1]

    return when {
        localPart.length <= 2 -> "${localPart.first()}***@$domain"
        else -> "${localPart.first()}***${localPart.takeLast(2)}@$domain"
    }
}