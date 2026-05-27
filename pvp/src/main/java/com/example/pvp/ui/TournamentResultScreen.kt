package com.example.pvp.ui

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.core_models.MatchOutcome
import com.example.core_models.TournamentResultState
import com.example.pvp.PvpViewModel

@Composable
fun TournamentResultScreen(
    viewModel: PvpViewModel,
    onPlayAgain: () -> Unit,
    onGoToProfile: () -> Unit
) {
    val result by viewModel.resultState.collectAsState()

    val state = result
    if (state == null) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }

    var visible by remember { mutableStateOf(false) }
    LaunchedEffect(Unit) { visible = true }

    AnimatedVisibility(
        visible = visible,
        enter = fadeIn(tween(400)) + slideInVertically(tween(400)) { it / 4 }
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            OutcomeIcon(outcome = state.outcome)

            Spacer(Modifier.height(16.dp))

            Text(
                text = when (state.outcome) {
                    MatchOutcome.WIN  -> "Победа!"
                    MatchOutcome.LOSE -> "Поражение"
                    MatchOutcome.DRAW -> "Ничья"
                },
                style = MaterialTheme.typography.headlineMedium,
                fontWeight = FontWeight.Bold,
                color = when (state.outcome) {
                    MatchOutcome.WIN  -> Color(0xFF4CAF50)
                    MatchOutcome.LOSE -> MaterialTheme.colorScheme.error
                    MatchOutcome.DRAW -> MaterialTheme.colorScheme.primary
                }
            )

            Spacer(Modifier.height(32.dp))

            ScoreCard(state = state)

            Spacer(Modifier.height(24.dp))

            RatingDeltaCard(
                myDelta       = state.myRatingDelta,
                outcome       = state.outcome
            )

            Spacer(Modifier.height(40.dp))

            Button(
                onClick = {
                    viewModel.clearResult()
                    onPlayAgain()
                },
                modifier = Modifier
                    .fillMaxWidth()
                    .height(52.dp),
                shape = RoundedCornerShape(14.dp)
            ) {
                Text("Играть снова", style = MaterialTheme.typography.titleMedium)
            }

            Spacer(Modifier.height(12.dp))

            OutlinedButton(
                onClick = {
                    viewModel.clearResult()
                    onGoToProfile()
                },
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(14.dp)
            ) {
                Text("В профиль")
            }
        }
    }
}

@Composable
private fun OutcomeIcon(outcome: MatchOutcome) {
    val scale by animateFloatAsState(
        targetValue = 1f,
        animationSpec = spring(
            dampingRatio = Spring.DampingRatioMediumBouncy,
            stiffness    = Spring.StiffnessLow
        ),
        label = "iconScale"
    )

    val emoji = when (outcome) {
        MatchOutcome.WIN  -> "🏆"
        MatchOutcome.LOSE -> "😔"
        MatchOutcome.DRAW -> "🤝"
    }

    val bgColor = when (outcome) {
        MatchOutcome.WIN  -> Color(0xFF4CAF50).copy(alpha = 0.12f)
        MatchOutcome.LOSE -> Color(0xFFF44336).copy(alpha = 0.12f)
        MatchOutcome.DRAW -> MaterialTheme.colorScheme.primaryContainer
    }

    Box(
        contentAlignment = Alignment.Center,
        modifier = Modifier
            .size(100.dp)
            .scale(scale)
            .background(bgColor, CircleShape)
    ) {
        Text(emoji, fontSize = 48.sp)
    }
}

@Composable
private fun ScoreCard(state: TournamentResultState) {
    Card(
        shape = RoundedCornerShape(20.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant
        ),
        modifier = Modifier.fillMaxWidth()
    ) {
        Column(
            modifier = Modifier.padding(20.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                "Счёт матча",
                style = MaterialTheme.typography.labelLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(Modifier.height(16.dp))

            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        "Вы",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        "${state.myScore}",
                        style = MaterialTheme.typography.displaySmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.primary
                    )
                    Text(
                        "очков",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                Text(
                    "vs",
                    style = MaterialTheme.typography.titleLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    fontWeight = FontWeight.Light
                )

                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Text(
                        "Соперник",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                    Text(
                        "${state.opponentScore}",
                        style = MaterialTheme.typography.displaySmall,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.secondary
                    )
                    Text(
                        "очков",
                        style = MaterialTheme.typography.labelSmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }
        }
    }
}

@Composable
private fun RatingDeltaCard(myDelta: Int, outcome: MatchOutcome) {
    val (bg, fg, sign) = when {
        myDelta > 0 -> Triple(
            Color(0xFF4CAF50).copy(alpha = 0.1f),
            Color(0xFF2E7D32),
            "+"
        )
        myDelta < 0 -> Triple(
            Color(0xFFF44336).copy(alpha = 0.1f),
            Color(0xFFC62828),
            ""
        )
        else -> Triple(
            MaterialTheme.colorScheme.surfaceVariant,
            MaterialTheme.colorScheme.onSurfaceVariant,
            "+"
        )
    }

    Card(
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(containerColor = bg),
        modifier = Modifier.fillMaxWidth()
    ) {
        Row(
            modifier = Modifier
                .padding(horizontal = 20.dp, vertical = 14.dp)
                .fillMaxWidth(),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Text(
                "Изменение рейтинга",
                style = MaterialTheme.typography.bodyMedium,
                color = fg
            )
            Text(
                "$sign$myDelta ELO",
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = fg
            )
        }
    }
}