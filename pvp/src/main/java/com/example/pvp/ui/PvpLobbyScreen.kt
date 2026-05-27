package com.example.pvp.ui

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.SportsEsports
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.core_models.PvpConnectionState
import com.example.core_models.PvpLobbyState
import com.example.pvp.PvpViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PvpLobbyScreen(
    viewModel: PvpViewModel,
    onNavigateBack: () -> Unit,
    onNavigateToTournament: (tournamentId: Int) -> Unit
) {
    val lobbyState by viewModel.lobbyState.collectAsState()
    val connectionState by viewModel.connectionState.collectAsState()
    val error by viewModel.error.collectAsState()

    if (lobbyState is PvpLobbyState.InviteExpired) {
        val reason = (lobbyState as PvpLobbyState.InviteExpired).reason
        AlertDialog(
            onDismissRequest = { viewModel.stopSearch() },
            shape = RoundedCornerShape(20.dp),
            title = { Text("Поиск завершён", fontWeight = FontWeight.Bold) },
            text  = { Text(reason) },
            confirmButton = {
                Button(
                    onClick = { viewModel.continueSearch() },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("Продолжить поиск") }
            },
            dismissButton = {
                OutlinedButton(
                    onClick = {
                        viewModel.stopSearch()
                        onNavigateBack()
                    },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("Выйти") }
            }
        )
    }

    LaunchedEffect(Unit) {
        viewModel.enterLobby()
    }

    val pendingTournamentId by viewModel.pendingTournamentId.collectAsState()

    LaunchedEffect(pendingTournamentId) {
        val id = pendingTournamentId ?: return@LaunchedEffect

        onNavigateToTournament(id)

        viewModel.clearPendingNavigation()
    }

    DisposableEffect(Unit) {
        onDispose { viewModel.leaveLobby() }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("PvP Турнир") },
                navigationIcon = {
                    IconButton(onClick = {
                        viewModel.cancelAndLeave()
                        onNavigateBack()
                    }) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Назад")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {

            val infiniteTransition = rememberInfiniteTransition(label = "pulse")
            val scale by infiniteTransition.animateFloat(
                initialValue = 1f,
                targetValue  = if (lobbyState == PvpLobbyState.Searching) 1.15f else 1f,
                animationSpec = infiniteRepeatable(
                    animation = tween(800, easing = EaseInOut),
                    repeatMode = RepeatMode.Reverse
                ),
                label = "scale"
            )

            Box(
                contentAlignment = Alignment.Center,
                modifier = Modifier
                    .size(120.dp)
                    .scale(scale)
                    .background(
                        color = MaterialTheme.colorScheme.primaryContainer,
                        shape = CircleShape
                    )
            ) {
                Icon(
                    imageVector = Icons.Default.SportsEsports,
                    contentDescription = null,
                    modifier = Modifier.size(56.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
            }

            Spacer(Modifier.height(40.dp))

            when {
                connectionState is PvpConnectionState.Connecting -> {
                    CircularProgressIndicator()
                    Spacer(Modifier.height(16.dp))
                    Text(
                        "Подключение к серверу...",
                        style = MaterialTheme.typography.bodyLarge,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                connectionState is PvpConnectionState.Error -> {
                    Text(
                        "Нет соединения с сервером",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.error,
                        fontWeight = FontWeight.SemiBold
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        (connectionState as PvpConnectionState.Error).message,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        textAlign = TextAlign.Center
                    )
                }

                lobbyState == PvpLobbyState.Searching -> {
                    Text(
                        "Поиск соперника",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Ищем игрока с похожим рейтингом\nпо вашей теме...",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        textAlign = TextAlign.Center
                    )
                    Spacer(Modifier.height(12.dp))
                    SearchingDotsIndicator()
                }

                lobbyState == PvpLobbyState.WaitingForOpponent -> {
                    Text(
                        "Приглашение принято!",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Ожидаем подтверждения соперника...",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        textAlign = TextAlign.Center
                    )
                    Spacer(Modifier.height(12.dp))
                    CircularProgressIndicator(modifier = Modifier.size(32.dp))
                }

                lobbyState == PvpLobbyState.InviteSent -> {
                    Text(
                        "Соперник найден!",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                    Spacer(Modifier.height(8.dp))
                    Text(
                        "Ожидаем подтверждения...",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Spacer(Modifier.height(12.dp))
                    CircularProgressIndicator(modifier = Modifier.size(32.dp))
                }

                else -> {
                    Text(
                        "Готов к бою?",
                        style = MaterialTheme.typography.headlineSmall,
                        fontWeight = FontWeight.Bold
                    )
                }
            }

            error?.let { msg ->
                Spacer(Modifier.height(16.dp))
                Card(
                    shape = RoundedCornerShape(12.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.5f)
                    )
                ) {
                    Text(
                        text = msg,
                        color = MaterialTheme.colorScheme.error,
                        style = MaterialTheme.typography.bodySmall,
                        modifier = Modifier.padding(12.dp),
                        textAlign = TextAlign.Center
                    )
                }
                LaunchedEffect(msg) {
                    kotlinx.coroutines.delay(4000)
                    viewModel.clearError()
                }
            }

            Spacer(Modifier.height(48.dp))

            if (lobbyState == PvpLobbyState.Searching) {
                OutlinedButton(
                    onClick = {
                        viewModel.cancelAndLeave()
                        onNavigateBack()
                    },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Отменить поиск")
                }
            }

            Spacer(Modifier.height(24.dp))
            RulesCard()
        }
    }
}

@Composable
private fun SearchingDotsIndicator() {
    val infiniteTransition = rememberInfiniteTransition(label = "dots")
    val dotIndex by infiniteTransition.animateFloat(
        initialValue = 0f,
        targetValue  = 3f,
        animationSpec = infiniteRepeatable(
            animation = tween(900, easing = LinearEasing),
            repeatMode = RepeatMode.Restart
        ),
        label = "dotIndex"
    )
    Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
        repeat(3) { i ->
            val alpha = if (dotIndex.toInt() == i) 1f else 0.3f
            Box(
                modifier = Modifier
                    .size(10.dp)
                    .background(
                        MaterialTheme.colorScheme.primary.copy(alpha = alpha),
                        CircleShape
                    )
            )
        }
    }
}

@Composable
private fun RulesCard() {
    Card(
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
        ),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                "Правила турнира",
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.SemiBold
            )
            Spacer(Modifier.height(8.dp))
            RuleRow("3 вопроса по вашей теме")
            RuleRow("45 секунд на каждый вопрос")
            RuleRow("Лёгкий: 5 очков  •  Средний: 10  •  Сложный: 15")
            RuleRow("Участие: +5 очков")
            RuleRow("Победа: + от 5 до 35")
            RuleRow("Ничья: +15")
            RuleRow("Поражение: −25")
        }
    }
}

@Composable
private fun RuleRow(text: String) {
    Row(
        verticalAlignment = Alignment.CenterVertically,
        modifier = Modifier.padding(vertical = 2.dp)
    ) {
        Box(
            modifier = Modifier
                .size(5.dp)
                .background(MaterialTheme.colorScheme.primary, CircleShape)
        )
        Spacer(Modifier.width(8.dp))
        Text(text, style = MaterialTheme.typography.bodySmall)
    }
}