package com.example.learning.ui

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.core.ui.UserEloHeader
import com.example.core_models.LevelsState
import com.example.core_models.dto.LevelDto
import com.example.learning.LearningViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LevelsScreen(
    viewModel: LearningViewModel,
    topicId: Int,
    topicName: String,
    onLevelClick: (levelId: Int) -> Unit,
    onBack: () -> Unit
) {
    val levelsState by viewModel.levelsState.collectAsState()
    val userElo     by viewModel.userElo.collectAsState()
    val userName    by viewModel.userName.collectAsState()

    LaunchedEffect(topicId) { viewModel.loadLevels(topicId) }
    DisposableEffect(Unit) { onDispose { viewModel.resetLevels() } }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = topicName,
                        style = MaterialTheme.typography.titleMedium
                            .copy(fontWeight = FontWeight.Bold)
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Назад")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .background(
                    Brush.verticalGradient(
                        colors = listOf(
                            MaterialTheme.colorScheme.primary.copy(alpha = 0.10f),
                            MaterialTheme.colorScheme.background,
                            MaterialTheme.colorScheme.primary.copy(alpha = 0.05f)
                        )
                    )
                )
        ) {
                    UserEloHeader(userName = userName, userElo = userElo)

                    when (val state = levelsState) {
                        is LevelsState.Loading, LevelsState.Idle -> {
                            Box(
                                modifier = Modifier.fillMaxSize(),
                                contentAlignment = Alignment.Center
                            ) {
                                CircularProgressIndicator()
                            }
                        }

                        is LevelsState.Error -> {
                            Box(
                                modifier = Modifier.fillMaxSize(),
                                contentAlignment = Alignment.Center
                            ) {
                                Column(
                                    horizontalAlignment = Alignment.CenterHorizontally
                                ) {
                                    Text(state.message, color = MaterialTheme.colorScheme.error)
                                    Spacer(Modifier.height(12.dp))
                                    Button(onClick = { viewModel.loadLevels(topicId) }) {
                                        Text("Повторить")
                                    }
                                }
                            }
                        }

                        is LevelsState.Success -> {
                                val levels = state.levels
                                val completedCount = levels.count { it.isCompleted }
                                val totalCount = levels.size

                                Column(
                                    modifier = Modifier
                                        .fillMaxSize()
                                        .weight(1f)
                                ) {

                                    // Прогресс по теме
                                    if (totalCount > 0) {
                                        Column(
                                            modifier = Modifier
                                                .fillMaxWidth()
                                                .padding(horizontal = 20.dp, vertical = 12.dp)
                                        ) {
                                            Row(
                                                modifier = Modifier.fillMaxWidth(),
                                                horizontalArrangement = Arrangement.SpaceBetween
                                            ) {
                                                Text(
                                                    text = "Прогресс темы",
                                                    style = MaterialTheme.typography.labelMedium,
                                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                                )
                                                Text(
                                                    text = "$completedCount / $totalCount уровней",
                                                    style = MaterialTheme.typography.labelMedium,
                                                    color = MaterialTheme.colorScheme.primary,
                                                    fontWeight = FontWeight.Medium
                                                )
                                            }
                                            Spacer(Modifier.height(6.dp))
                                            LinearProgressIndicator(
                                                progress = { completedCount.toFloat() / totalCount },
                                                modifier = Modifier
                                                    .fillMaxWidth()
                                                    .height(6.dp)
                                                    .clip(RoundedCornerShape(3.dp)),
                                                color = MaterialTheme.colorScheme.primary,
                                                trackColor = MaterialTheme.colorScheme.outline.copy(
                                                    alpha = 0.2f
                                                )
                                            )
                                        }
                                        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant)
                                    }

                                    LazyColumn(
                                        contentPadding = PaddingValues(
                                            horizontal = 16.dp,
                                            vertical = 24.dp
                                        ),
                                        verticalArrangement = Arrangement.spacedBy(8.dp),
                                        modifier = Modifier.fillMaxSize()
                                    ) {
                                        itemsIndexed(
                                            levels,
                                            key = { _, level -> level.id }) { index, level ->

                                            Box(
                                                modifier = Modifier
                                                    .fillMaxWidth(),
                                                contentAlignment = Alignment.Center
                                            ) {
                                                LevelCircle(
                                                    level = level,
                                                    onClick = {
                                                        if (!level.isLocked) onLevelClick(level.id)
                                                    }
                                                )
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
        }

@Composable
private fun LevelCircle(
    level: LevelDto,
    onClick: () -> Unit
) {

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(
                enabled = !level.isLocked,
                onClick = onClick
            ),
        shape = RoundedCornerShape(16.dp),
        border = if (!level.isLocked && !level.isCompleted) {
            BorderStroke(
                width = 1.dp,
                color = MaterialTheme.colorScheme.primary
            )
        } else null,
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
        colors = CardDefaults.cardColors(
            containerColor = when {
                level.isCompleted ->
                    MaterialTheme.colorScheme.primaryContainer

                else ->
                    MaterialTheme.colorScheme.primary.copy(alpha = 0.12f)
            }
        )
    ) {

        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {

            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {

                Text(
                    text = "Уровень ${level.levelNumber}",
                    style = MaterialTheme.typography.titleMedium.copy(
                        fontWeight = FontWeight.SemiBold
                    ),
                    color = if (level.isLocked)
                        MaterialTheme.colorScheme.onSurfaceVariant
                    else
                        MaterialTheme.colorScheme.onSurface
                )

                when {

                    level.isLocked -> {
                        Icon(
                            Icons.Default.Lock,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(20.dp)
                        )
                    }

                    level.isCompleted -> {
                        Icon(
                            Icons.Default.CheckCircle,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(20.dp)
                        )
                    }
                }
            }

            if (!level.isLocked && level.taskCount > 0) {

                Spacer(Modifier.height(10.dp))

                LinearProgressIndicator(
                    progress = {
                        level.tasksCompleted.toFloat() / level.taskCount.toFloat()
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(4.dp)
                        .clip(RoundedCornerShape(2.dp)),
                    color = MaterialTheme.colorScheme.primary,
                    trackColor = MaterialTheme.colorScheme.outline.copy(alpha = 0.2f)
                )

                Spacer(Modifier.height(6.dp))

                Text(
                    text = "${level.tasksCompleted} / ${level.taskCount} заданий",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.primary
                )
            }

            if (level.hasTheory && !level.isLocked) {

                Spacer(Modifier.height(6.dp))

                Text(
                    text = "Есть теория",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            if (level.isLocked) {

                Spacer(Modifier.height(6.dp))

                Text(
                    text = "Завершите предыдущий уровень",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}