package com.example.learning.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.core_models.TopicsState
import com.example.core_models.dto.TopicDto
import com.example.core.ui.UserEloHeader
import com.example.learning.LearningViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LobbyScreen(
    viewModel: LearningViewModel,
    onTopicClick: (topicId: Int, topicName: String) -> Unit,
    onBack: () -> Unit
) {
    val topicsState by viewModel.topicsState.collectAsState()
    val userElo  by viewModel.userElo.collectAsState()
    val userName by viewModel.userName.collectAsState()

    LaunchedEffect(Unit) { viewModel.loadTopics() }

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        "Темы",
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
            UserEloHeader(
                userName = userName,
                userElo  = userElo
            )

            when (val state = topicsState) {
                is TopicsState.Loading, TopicsState.Idle -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        CircularProgressIndicator()
                    }
                }
                is TopicsState.Error -> {
                    Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(state.message, color = MaterialTheme.colorScheme.error)
                            Spacer(Modifier.height(12.dp))
                            Button(onClick = { viewModel.loadTopics() }) { Text("Повторить") }
                        }
                    }
                }
                is TopicsState.Success -> {
                    LazyColumn(
                        modifier = Modifier.fillMaxSize(),
                        contentPadding = PaddingValues(16.dp),
                        verticalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        items(state.topics, key = { it.id }) { topic ->
                            TopicCard(
                                topic = topic,
                                onClick = {
                                    if (!topic.isLocked) onTopicClick(topic.id, topic.name)
                                }
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun TopicCard(topic: TopicDto, onClick: () -> Unit) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .then(if (!topic.isLocked) Modifier.clickable(onClick = onClick) else Modifier),
        shape = RoundedCornerShape(16.dp),
        border = if (!topic.isLocked && !topic.isCompleted) {
            BorderStroke(
                width = 1.dp,
                color = MaterialTheme.colorScheme.primary
            )
        } else null,
        elevation = CardDefaults.cardElevation(defaultElevation = 0.dp),
        colors = CardDefaults.cardColors(
            containerColor = when {
                topic.isCompleted -> MaterialTheme.colorScheme.primaryContainer
                topic.isLocked    -> MaterialTheme.colorScheme.primary.copy(alpha = 0.12f)
                else              -> MaterialTheme.colorScheme.primary.copy(alpha = 0.12f)
            }
        )
    ) {
        Column(modifier = Modifier.fillMaxWidth().padding(16.dp)) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Text(
                    text = topic.name,
                    style = MaterialTheme.typography.titleMedium.copy(fontWeight = FontWeight.SemiBold),
                    modifier = Modifier.weight(1f),
                    color = if (topic.isLocked) MaterialTheme.colorScheme.onSurfaceVariant
                    else MaterialTheme.colorScheme.onSurface
                )
                Spacer(Modifier.width(8.dp))
                when {
                    topic.isLocked -> Icon(Icons.Default.Lock, null,
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(20.dp))
                    topic.isCompleted -> Icon(Icons.Default.CheckCircle, null,
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(20.dp))
                    topic.levelCount > 0 && topic.completedLevelCount > 0 -> Text(
                        text = "${(topic.topicProgress * 100).toInt()}%",
                        style = MaterialTheme.typography.labelMedium,
                        color = MaterialTheme.colorScheme.primary)
                }
            }

            val description = topic.description
            if (!description.isNullOrBlank() && !topic.isLocked) {
                Spacer(Modifier.height(4.dp))
                Text(text = description, style = MaterialTheme.typography.bodySmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant, maxLines = 2)
            }

            if (topic.isLocked) {
                Spacer(Modifier.height(6.dp))
                Text(text = "Завершите предыдущую тему для открытия",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant)
            } else {
                Spacer(Modifier.height(10.dp))
                if (topic.levelCount > 0) {
                    LinearProgressIndicator(
                        progress = { topic.topicProgress },
                        modifier = Modifier.fillMaxWidth().height(4.dp).clip(RoundedCornerShape(2.dp)),
                        color = if (topic.isCompleted) MaterialTheme.colorScheme.primary
                        else MaterialTheme.colorScheme.primary,
                        trackColor = MaterialTheme.colorScheme.outline.copy(alpha = 0.2f)
                    )
                    Spacer(Modifier.height(6.dp))
                }
                Text(text = "${topic.completedLevelCount} / ${topic.levelCount} уровней",
                    style = MaterialTheme.typography.labelMedium,
                    color = MaterialTheme.colorScheme.primary)
            }
        }
    }
}