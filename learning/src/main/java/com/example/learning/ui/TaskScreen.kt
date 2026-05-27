package com.example.learning.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Send
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.core_models.RunState
import com.example.core_models.SubmitState
import com.example.core_models.TaskLoadState
import com.example.core_models.dto.HintDto
import com.example.core_models.dto.SubmitResponseDto
import com.example.core_models.dto.TaskDetailDto
import com.example.core_models.dto.TestResultDto
import com.example.core.ui.UserEloHeader
import com.example.learning.TaskViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TaskScreen(
    viewModel: TaskViewModel,
    taskId: Int,
    userName: String = "",
    userElo: Int = 0,
    onBack: () -> Unit,
    onTaskSolved: () -> Unit
) {
    val taskState     by viewModel.taskState.collectAsState()
    val runState      by viewModel.runState.collectAsState()
    val submitState   by viewModel.submitState.collectAsState()
    val code          by viewModel.code.collectAsState()
    val hintsRevealed by viewModel.hintsRevealed.collectAsState()

    LaunchedEffect(taskId) { viewModel.loadTask(taskId) }
    DisposableEffect(Unit) { onDispose { viewModel.reset() } }

    val submitSuccess = (submitState as? SubmitState.Success)?.result
    if (submitSuccess != null) {
        SubmitResultDialog(
            result = submitSuccess,
            onDismiss = {
                viewModel.resetSubmit()
                if (submitSuccess.isCorrect) {
                    onTaskSolved()
                }
            }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    val title = when (val s = taskState) {
                        is TaskLoadState.Success -> s.task.name
                        else -> "Задание"
                    }
                    Text(
                        title,
                        style = MaterialTheme.typography.titleMedium
                            .copy(fontWeight = FontWeight.Bold)
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, "Назад")
                    }
                },
                actions = {
                    val task = (taskState as? TaskLoadState.Success)?.task
                    if (task != null) {
                        DifficultyBadge(difficulty = task.difficulty)
                        Spacer(Modifier.width(8.dp))
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            UserEloHeader(userName = userName, userElo = userElo)

            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(MaterialTheme.colorScheme.background)
            ) {
                when (val state = taskState) {
                    is TaskLoadState.Loading, TaskLoadState.Idle ->
                        CircularProgressIndicator(Modifier.align(Alignment.Center))

                    is TaskLoadState.Error -> Column(
                        modifier = Modifier.align(Alignment.Center),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Text(state.message, color = MaterialTheme.colorScheme.error)
                        Spacer(Modifier.height(12.dp))
                        Button(onClick = { viewModel.loadTask(taskId) }) { Text("Повторить") }
                    }

                    is TaskLoadState.Success -> TaskContent(
                        task = state.task,
                        code = code,
                        runState = runState,
                        submitState = submitState,
                        hintsRevealed = hintsRevealed,
                        onCodeChange = { viewModel.updateCode(it) },
                        onRun = { viewModel.runCode(taskId) },
                        onSubmit = { viewModel.submitSolution(taskId) },
                        onRevealHint = { viewModel.revealNextHint() }
                    )
                }
            }
        }
    }
}

@Composable
private fun TaskContent(
    task: TaskDetailDto,
    code: String,
    runState: RunState,
    submitState: SubmitState,
    hintsRevealed: Int,
    onCodeChange: (String) -> Unit,
    onRun: () -> Unit,
    onSubmit: () -> Unit,
    onRevealHint: () -> Unit
) {
    val scrollState = rememberScrollState()
    val isLoading = runState is RunState.Running || submitState is SubmitState.Submitting

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(scrollState)
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(
                containerColor = MaterialTheme.colorScheme.primary.copy(alpha = 0.08f)
            )
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Условие",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.primary
                    )
                    if (task.isSolved) {
                        Icon(
                            Icons.Default.CheckCircle,
                            contentDescription = "Решено",
                            tint = MaterialTheme.colorScheme.primary,
                            modifier = Modifier.size(16.dp)
                        )
                    }
                }
                Spacer(Modifier.height(8.dp))
                Text(
                    text = task.condition,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
        }

        Column {
            Text(
                text = "Ваше решение",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Spacer(Modifier.height(6.dp))
            OutlinedTextField(
                value = code,
                onValueChange = onCodeChange,
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(min = 200.dp, max = 400.dp),
                textStyle = MaterialTheme.typography.bodySmall.copy(
                    fontFamily = FontFamily.Monospace,
                    fontSize = 13.sp
                ),
                placeholder = {
                    Text(
                        "fun main() {\n    // Ваш код здесь\n}",
                        fontFamily = FontFamily.Monospace,
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.5f)
                    )
                },
                shape = RoundedCornerShape(12.dp)
            )
        }

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            OutlinedButton(
                onClick = onRun,
                enabled = !isLoading && code.isNotBlank(),
                modifier = Modifier.weight(1f)
            ) {
                if (runState is RunState.Running) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp
                    )
                } else {
                    Icon(Icons.Default.PlayArrow, null, Modifier.size(18.dp))
                }
                Spacer(Modifier.width(4.dp))
                Text("Запустить")
            }

            Button(
                onClick = onSubmit,
                enabled = !isLoading && code.isNotBlank() && !task.isSolved,
                modifier = Modifier.weight(1f)
            ) {
                if (submitState is SubmitState.Submitting) {
                    CircularProgressIndicator(
                        modifier = Modifier.size(16.dp),
                        strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary
                    )
                } else {
                    Icon(Icons.Default.Send, null, Modifier.size(18.dp))
                }
                Spacer(Modifier.width(4.dp))
                Text(if (task.isSolved) "Решено" else "Проверить")
            }
        }

        when (runState) {
            is RunState.Success -> {
                val result = runState.result
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.surfaceVariant
                    )
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween,
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text(
                                text = "Вывод программы",
                                style = MaterialTheme.typography.labelMedium,
                                color = MaterialTheme.colorScheme.onSurfaceVariant
                            )
                            Text(
                                text = if (result.isSuccess) "✓ OK" else "✗ Ошибка",
                                style = MaterialTheme.typography.labelSmall,
                                color = if (result.isSuccess)
                                    MaterialTheme.colorScheme.primary
                                else
                                    MaterialTheme.colorScheme.error
                            )
                        }
                        Spacer(Modifier.height(6.dp))
                        val isCompileError = result.compileOutput.isNotBlank()
                        val outputText = when {
                            isCompileError             -> result.compileOutput
                            result.stderr.isNotBlank() -> result.stderr
                            result.stdout.isNotBlank() -> result.stdout
                            else                       -> "(нет вывода)"
                        }
                        val outputColor = when {
                            isCompileError || result.stderr.isNotBlank() ->
                                MaterialTheme.colorScheme.error
                            result.isSuccess ->
                                MaterialTheme.colorScheme.onSurface
                            else ->
                                MaterialTheme.colorScheme.error
                        }
                        Text(
                            text = outputText,
                            style = MaterialTheme.typography.bodySmall.copy(
                                fontFamily = FontFamily.Monospace
                            ),
                            color = outputColor
                        )
                    }
                }
            }
            is RunState.Error -> {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.4f)
                    )
                ) {
                    Row(
                        modifier = Modifier.padding(12.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Icon(
                            Icons.Default.Info,
                            contentDescription = null,
                            tint = MaterialTheme.colorScheme.error,
                            modifier = Modifier.size(16.dp)
                        )
                        Text(
                            text = runState.message,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodySmall
                        )
                    }
                }
            }
            else -> {}
        }

        if (task.hints.isNotEmpty()) {
            HintsSection(
                hints = task.hints,
                revealed = hintsRevealed,
                onReveal = onRevealHint
            )
        }

        Spacer(Modifier.height(32.dp))
    }
}

@Composable
private fun HintsSection(
    hints: List<HintDto>,
    revealed: Int,
    onReveal: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.secondaryContainer.copy(alpha = 0.5f)
        )
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            Text(
                text = "Подсказки",
                style = MaterialTheme.typography.labelMedium,
                color = MaterialTheme.colorScheme.secondary
            )
            Spacer(Modifier.height(8.dp))

            hints.take(revealed).forEachIndexed { index, hint ->
                if (index > 0) {
                    HorizontalDivider(
                        modifier = Modifier.padding(vertical = 6.dp),
                        color = MaterialTheme.colorScheme.outlineVariant
                    )
                }
                Row(
                    modifier = Modifier.padding(vertical = 2.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Icon(
                        Icons.Default.Info,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.secondary,
                        modifier = Modifier.size(18.dp).padding(top = 2.dp)
                    )
                    Text(
                        text = hint.hintText,
                        style = MaterialTheme.typography.bodySmall
                    )
                }
            }

            if (revealed < hints.size) {
                if (revealed > 0) Spacer(Modifier.height(8.dp))
                OutlinedButton(
                    onClick = onReveal,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Icon(Icons.Default.Info, null, Modifier.size(16.dp))
                    Spacer(Modifier.width(6.dp))
                    Text(
                        text = if (revealed == 0) "Показать подсказку"
                        else "Следующая подсказка (${revealed + 1}/${hints.size})"
                    )
                }
            } else {
                Spacer(Modifier.height(4.dp))
                Text(
                    text = "Все подсказки показаны",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }
    }
}

@Composable
private fun SubmitResultDialog(
    result: SubmitResponseDto,
    onDismiss: () -> Unit
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = {
            Text(
                text = if (result.isCorrect)
                    "Задание решено!"
                else
                    "Не все тесты пройдены",
                fontWeight = FontWeight.Bold,
                color = if (result.isCorrect)
                    MaterialTheme.colorScheme.primary
                else
                    MaterialTheme.colorScheme.error,
                modifier = Modifier.fillMaxWidth(),
                textAlign = androidx.compose.ui.text.style.TextAlign.Center
            )
        },

        text = {
            Column(
                verticalArrangement = Arrangement.spacedBy(8.dp),
                horizontalAlignment = Alignment.CenterHorizontally,
                modifier = Modifier.fillMaxWidth()
            ) {

                if (result.totalCount > 0) {
                    Text(
                        text = "Пройдено тестов: ${result.passedCount} / ${result.totalCount}",
                        style = MaterialTheme.typography.bodyMedium,
                        textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                        modifier = Modifier.fillMaxWidth()
                    )
                }

                if (result.eloGained > 0) {
                    Text(
                        text = "+${result.eloGained} очков ELO",
                        style = MaterialTheme.typography.bodyMedium.copy(
                            fontWeight = FontWeight.Bold
                        ),
                        color = MaterialTheme.colorScheme.primary,
                        textAlign = androidx.compose.ui.text.style.TextAlign.Center,
                        modifier = Modifier.fillMaxWidth()
                    )
                }

                Text(
                    text = result.message,
                    style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                val failedPublic = result.testResults.filter {
                    !it.passed && !it.isHidden
                }

                if (failedPublic.isNotEmpty()) {
                    Spacer(Modifier.height(4.dp))

                    failedPublic.forEachIndexed { index, testResult ->
                        FailedTestDetail(
                            index = index + 1,
                            result = testResult
                        )
                    }
                }

                val failedHidden = result.testResults.count {
                    !it.passed && it.isHidden
                }

                if (failedHidden > 0) {
                    Text(
                        text = "Скрытых тестов не пройдено: $failedHidden",
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.error
                    )
                }
            }
        },

        confirmButton = {
            Button(onClick = onDismiss) {
                Text(
                    if (result.isCorrect)
                        "Отлично!"
                    else
                        "Попробовать снова"
                )
            }
        }
    )
}

@Composable
private fun FailedTestDetail(index: Int, result: TestResultDto) {
    Column(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(8.dp))
            .background(MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.3f))
            .padding(8.dp),
        verticalArrangement = Arrangement.spacedBy(4.dp)
    ) {
        Text(
            text = "Тест $index: не пройден",
            style = MaterialTheme.typography.labelMedium.copy(fontWeight = FontWeight.Bold),
            color = MaterialTheme.colorScheme.error
        )
        result.errorMessage?.let { err ->
            Text(
                text = err.trim(),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.error
            )
        }
        val expectedOutput = result.expectedOutput
        val actualOutput = result.actualOutput
        if (expectedOutput != null && actualOutput != null) {
            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(
                    text = "Ожидалось:",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = expectedOutput.take(60),
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontFamily = FontFamily.Monospace
                    ),
                    color = MaterialTheme.colorScheme.onSurface
                )
            }
            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                Text(
                    text = "Получено: ",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                Text(
                    text = if (actualOutput.isBlank()) "(пусто)" else actualOutput.take(60),
                    style = MaterialTheme.typography.labelSmall.copy(
                        fontFamily = FontFamily.Monospace
                    ),
                    color = MaterialTheme.colorScheme.error
                )
            }
        }
    }
}

@Composable
private fun DifficultyBadge(difficulty: String) {
    val color = when (difficulty.lowercase()) {
        "лёгкий", "легкий" -> MaterialTheme.colorScheme.tertiary
        "средний"           -> MaterialTheme.colorScheme.secondary
        "сложный"           -> MaterialTheme.colorScheme.error
        else                -> MaterialTheme.colorScheme.outline
    }
    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(6.dp))
            .background(color.copy(alpha = 0.15f))
            .padding(horizontal = 8.dp, vertical = 3.dp)
    ) {
        Text(
            text = difficulty,
            style = MaterialTheme.typography.labelSmall,
            color = color
        )
    }
}