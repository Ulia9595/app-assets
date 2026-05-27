package com.example.learning.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.ChevronRight
import androidx.compose.material.icons.outlined.MenuBook
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.core_models.LevelDetailState
import com.example.core_models.dto.PracticeTaskDto
import com.example.core.ui.UserEloHeader
import com.example.learning.LearningViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LevelDetailScreen(
    viewModel: LearningViewModel,
    levelId: Int,
    onBack: () -> Unit,
    onOpenTheory: (title: String, content: String) -> Unit,
    onOpenTask: (taskId: Int) -> Unit
) {
    val detailState by viewModel.levelDetailState.collectAsState()
    val userElo     by viewModel.userElo.collectAsState()
    val userName    by viewModel.userName.collectAsState()

    LaunchedEffect(levelId) { viewModel.loadLevelDetail(levelId) }
    DisposableEffect(Unit) { onDispose { viewModel.resetDetail() } }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    val title = when (val s = detailState) {
                        is LevelDetailState.Success -> s.detail.name
                        else -> "Уровень"
                    }
                    Text(
                        text = title,
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
                .background(MaterialTheme.colorScheme.background)
        ) {
            UserEloHeader(userName = userName, userElo = userElo)

            when (val state = detailState) {
                is LevelDetailState.Loading, LevelDetailState.Idle -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator()
                    }
                }

                is LevelDetailState.Error -> {
                    Box(
                        modifier = Modifier.fillMaxSize(),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(state.message, color = MaterialTheme.colorScheme.error)
                            Spacer(Modifier.height(12.dp))
                            Button(onClick = { viewModel.loadLevelDetail(levelId) }) {
                                Text("Повторить")
                            }
                        }
                    }
                }

                is LevelDetailState.Success -> {
                    val detail = state.detail
                    val theory = detail.theory
                    val tasks  = detail.tasks
                    val solvedCount = tasks.count { it.isSolved }

                    LazyColumn(
                        modifier = Modifier
                            .fillMaxSize()
                            .weight(1f),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        if (theory != null) {
                            item(key = "theory_btn") {
                                TheoryButton(
                                    title = theory.title,
                                    onClick = { onOpenTheory(theory.title, theory.content) }
                                )
                            }
                        }

                        item(key = "tasks_header") {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                verticalAlignment = Alignment.CenterVertically,
                                horizontalArrangement = Arrangement.SpaceBetween
                            ) {
                                Text(
                                    text = "Задания",
                                    style = MaterialTheme.typography.titleMedium
                                        .copy(fontWeight = FontWeight.Bold)
                                )
                                if (tasks.isNotEmpty()) {
                                    Text(
                                        text = "$solvedCount / ${tasks.size} выполнено",
                                        style = MaterialTheme.typography.labelMedium,
                                        color = if (solvedCount == tasks.size)
                                            MaterialTheme.colorScheme.primary
                                        else
                                            MaterialTheme.colorScheme.onSurfaceVariant
                                    )
                                }
                            }
                        }

                        if (tasks.isNotEmpty()) {
                            item(key = "tasks_progress") {
                                LinearProgressIndicator(
                                    progress = { solvedCount.toFloat() / tasks.size },
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .height(4.dp)
                                        .clip(RoundedCornerShape(2.dp)),
                                    color = MaterialTheme.colorScheme.primary,
                                    trackColor = MaterialTheme.colorScheme.outline
                                        .copy(alpha = 0.2f)
                                )
                            }
                        }

                        if (tasks.isNotEmpty()) {
                            items(tasks, key = { "task_${it.id}" }) { task ->
                                TaskCard(
                                    task = task,
                                    onClick = { onOpenTask(task.id) }
                                )
                            }
                        } else {
                            item(key = "no_tasks") {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(top = 32.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Text(
                                        text = "Задания для этого уровня ещё не добавлены",
                                        color = MaterialTheme.colorScheme.onSurfaceVariant
                                    )
                                }
                            }
                        }

                        if (theory == null && tasks.isEmpty()) {
                            item(key = "empty") {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(top = 64.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Text(
                                        text = "Контент для этого уровня ещё не добавлен",
                                        color = MaterialTheme.colorScheme.onSurfaceVariant
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
private fun TheoryButton(title: String, onClick: () -> Unit) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.primaryContainer
        ),
        onClick = onClick
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Icon(
                imageVector = Icons.Outlined.MenuBook,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(28.dp)
            )
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = "Читать теорию",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.primary
                )
                Text(
                    text = title,
                    style = MaterialTheme.typography.bodyMedium
                        .copy(fontWeight = FontWeight.Medium),
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
            }
            Icon(
                imageVector = Icons.Default.ChevronRight,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}

@Composable
private fun TaskCard(task: PracticeTaskDto, onClick: () -> Unit) {
    val difficultyColor = when (task.difficulty.lowercase()) {
        "лёгкий", "легкий", "easy" -> MaterialTheme.colorScheme.tertiary
        "средний", "medium"         -> MaterialTheme.colorScheme.secondary
        "сложный", "hard"           -> MaterialTheme.colorScheme.error
        else                        -> MaterialTheme.colorScheme.outline
    }

    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (task.isSolved)
                MaterialTheme.colorScheme.primary.copy(alpha = 0.10f)
            else
                MaterialTheme.colorScheme.primary.copy(alpha = 0.08f)
        )
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(6.dp)
                ) {
                    Text(
                        text = task.name,
                        style = MaterialTheme.typography.bodyMedium
                            .copy(fontWeight = FontWeight.Medium)
                    )

                    if (task.isSolved) {
                        Icon(
                            imageVector = Icons.Default.CheckCircle,
                            contentDescription = "Решено",
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(16.dp)
                        )
                    }
                }
                Spacer(Modifier.height(4.dp))
                Text(
                    text = task.condition,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    maxLines = 2
                )
            }
            Spacer(Modifier.width(12.dp))
            Column(
                horizontalAlignment = Alignment.End,
                verticalArrangement = Arrangement.spacedBy(4.dp)
            ) {
                Box(
                    modifier = Modifier
                        .clip(RoundedCornerShape(8.dp))
                        .background(difficultyColor.copy(alpha = 0.15f))
                        .padding(horizontal = 8.dp, vertical = 4.dp)
                ) {
                    Text(
                        text = task.difficulty,
                        style = MaterialTheme.typography.labelSmall,
                        color = difficultyColor
                    )
                }
                Icon(
                    imageVector = Icons.Default.ChevronRight,
                    contentDescription = null,
                    tint = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.size(18.dp)
                )
            }
        }
    }
}