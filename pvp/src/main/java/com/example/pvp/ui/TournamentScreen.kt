package com.example.pvp.ui

import androidx.compose.animation.*
import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Timer
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.core_models.AnswerState
import com.example.core_models.TournamentState
import com.example.core_models.dto.PvpAnswerOptionDto
import com.example.core_models.dto.PvpQuestionDto
import com.example.pvp.PvpViewModel

@Composable
fun TournamentScreen(
    viewModel: PvpViewModel,
    onNavigateToResult: () -> Unit
) {
    val tournamentState by viewModel.tournamentState.collectAsState()
    val error by viewModel.error.collectAsState()

    LaunchedEffect(Unit) {
        viewModel.navigateToResult.collect { onNavigateToResult() }
    }

    val state = tournamentState

    if (state == null) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            CircularProgressIndicator()
        }
        return
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        TournamentHeader(
            current       = state.currentQuestionIndex + 1,
            total         = state.questions.size,
            timerSeconds  = state.timerSeconds,
            opponentName  = state.opponentName.ifEmpty { "Соперник" },
            myScore       = state.myTotalScore
        )

        Spacer(Modifier.height(24.dp))

        if (state.currentQuestion != null) {
            AnimatedContent(
                targetState = state.currentQuestionIndex,
                transitionSpec = {
                    (slideInHorizontally { it } + fadeIn()).togetherWith(
                        slideOutHorizontally { -it } + fadeOut()
                    )
                },
                label = "question"
            ) { questionIndex ->
                val question = state.questions.getOrNull(questionIndex) ?: return@AnimatedContent
                val alreadyAnswered = state.myAnswers[question.id]

                QuestionCard(
                    question         = question,
                    answerState      = alreadyAnswered,
                    timerSeconds     = state.timerSeconds,
                    onOptionSelected = { optionId ->
                        if (alreadyAnswered == null && state.timerSeconds > 0) {
                            viewModel.submitAnswer(optionId)
                        }
                    }
                )
            }
        }

        error?.let { msg ->
            Spacer(Modifier.height(12.dp))
            Text(
                text  = msg,
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodySmall,
                textAlign = TextAlign.Center,
                modifier = Modifier.fillMaxWidth()
            )
        }
    }
}

@Composable
private fun TournamentHeader(
    current: Int,
    total: Int,
    timerSeconds: Int,
    opponentName: String,
    myScore: Int
) {
    Column {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                "Вопрос $current из $total",
                style = MaterialTheme.typography.labelLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                "Мои очки: $myScore",
                style = MaterialTheme.typography.labelLarge,
                fontWeight = FontWeight.SemiBold,
                color = MaterialTheme.colorScheme.primary
            )
        }

        Spacer(Modifier.height(8.dp))

        LinearProgressIndicator(
            progress = { current.toFloat() / total },
            modifier = Modifier
                .fillMaxWidth()
                .height(6.dp)
                .clip(CircleShape),
            color = MaterialTheme.colorScheme.primary,
            trackColor = MaterialTheme.colorScheme.outlineVariant,
            strokeCap = StrokeCap.Round
        )

        Spacer(Modifier.height(12.dp))

        TimerRow(timerSeconds = timerSeconds)

        Spacer(Modifier.height(4.dp))

        Text(
            "vs $opponentName",
            style = MaterialTheme.typography.bodySmall,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@Composable
private fun TimerRow(timerSeconds: Int) {
    val timerColor = when {
        timerSeconds > 20 -> MaterialTheme.colorScheme.primary
        timerSeconds > 10 -> Color(0xFFF57C00)
        else              -> MaterialTheme.colorScheme.error
    }

    val scale by animateFloatAsState(
        targetValue = if (timerSeconds <= 10) 1.1f else 1f,
        animationSpec = spring(stiffness = Spring.StiffnessMedium),
        label = "timerScale"
    )

    Row(
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp)
    ) {
        Icon(
            Icons.Default.Timer,
            contentDescription = "Таймер",
            tint = timerColor,
            modifier = Modifier.size(18.dp)
        )
        Text(
            text = "$timerSeconds с",
            style = MaterialTheme.typography.titleMedium,
            fontWeight = FontWeight.Bold,
            color = timerColor,
            modifier = Modifier
                .animateContentSize()
        )

        Spacer(Modifier.width(8.dp))

        LinearProgressIndicator(
            progress = { timerSeconds / 45f },
            modifier = Modifier
                .weight(1f)
                .height(8.dp)
                .clip(CircleShape),
            color = timerColor,
            trackColor = MaterialTheme.colorScheme.outlineVariant,
            strokeCap = StrokeCap.Round
        )
    }
}

@Composable
private fun QuestionCard(
    question: PvpQuestionDto,
    answerState: AnswerState?,
    timerSeconds: Int,
    onOptionSelected: (Int) -> Unit
) {
    val isTimeOut = timerSeconds <= 0 && answerState == null

    Column {
        Row(
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
            modifier = Modifier.padding(bottom = 12.dp)
        ) {
            DifficultyChip(question.difficulty)
            Text(
                "+${question.points} очков",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.secondary
            )
        }

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(
                containerColor = MaterialTheme.colorScheme.surfaceVariant
            ),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(
                text = question.text,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.SemiBold,
                modifier = Modifier.padding(16.dp),
                textAlign = TextAlign.Start
            )
        }

        Spacer(Modifier.height(16.dp))

        if (isTimeOut) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 20.dp)
                    .background(
                        color = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.8f),
                        shape = RoundedCornerShape(12.dp)
                    )
                    .border(1.dp, MaterialTheme.colorScheme.error, RoundedCornerShape(12.dp))
                    .padding(16.dp),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "Время ответа истекло",
                    color = MaterialTheme.colorScheme.onErrorContainer,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
            }
        } else {
            question.options.forEach { option ->
                AnswerOptionItem(
                    option      = option,
                    answerState = answerState,
                    onClick     = { if (timerSeconds > 0) onOptionSelected(option.id) }
                )
                Spacer(Modifier.height(8.dp))
            }
        }
    }
}

@Composable
private fun AnswerOptionItem(
    option: PvpAnswerOptionDto,
    answerState: AnswerState?,
    onClick: () -> Unit
) {
    val isSelected = answerState?.selectedOptionId == option.id
    val isCorrect  = answerState?.correctOptionId  == option.id
    val answered   = answerState != null

    val containerColor = when {
        answered && isCorrect  -> Color(0xFF4CAF50).copy(alpha = 0.15f)
        answered && isSelected && !isCorrect -> MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.4f)
        isSelected             -> MaterialTheme.colorScheme.primaryContainer
        else                   -> MaterialTheme.colorScheme.surface
    }

    val borderColor = when {
        answered && isCorrect  -> Color(0xFF4CAF50)
        answered && isSelected && !isCorrect -> MaterialTheme.colorScheme.error
        isSelected             -> MaterialTheme.colorScheme.primary
        else                   -> MaterialTheme.colorScheme.outlineVariant
    }

    val textColor = when {
        answered && isCorrect  -> Color(0xFF2E7D32)
        answered && isSelected && !isCorrect -> MaterialTheme.colorScheme.error
        else                   -> MaterialTheme.colorScheme.onSurface
    }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(containerColor)
            .border(
                width = if (isSelected || (answered && isCorrect)) 2.dp else 1.dp,
                color = borderColor,
                shape = RoundedCornerShape(12.dp)
            )
            .clickable(enabled = !answered) { onClick() }
            .padding(horizontal = 16.dp, vertical = 14.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text  = option.text,
            style = MaterialTheme.typography.bodyMedium,
            color = textColor,
            modifier = Modifier.weight(1f)
        )

        if (answered && isCorrect) {
            Text("✓", color = Color(0xFF2E7D32), fontWeight = FontWeight.Bold)
        } else if (answered && isSelected && !isCorrect) {
            Text("✗", color = MaterialTheme.colorScheme.error, fontWeight = FontWeight.Bold)
        }
    }
}

@Composable
private fun DifficultyChip(difficulty: String) {
    val (color, label) = when {
        difficulty.contains("лёгк", ignoreCase = true) ||
                difficulty.contains("легк", ignoreCase = true)  -> Color(0xFF4CAF50) to "Лёгкий"
        difficulty.contains("средн", ignoreCase = true) -> Color(0xFFF57C00) to "Средний"
        else                                             -> Color(0xFFF44336) to "Сложный"
    }

    Surface(
        shape = RoundedCornerShape(8.dp),
        color = color.copy(alpha = 0.12f)
    ) {
        Text(
            text = label,
            color = color,
            style = MaterialTheme.typography.labelSmall,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
        )
    }
}