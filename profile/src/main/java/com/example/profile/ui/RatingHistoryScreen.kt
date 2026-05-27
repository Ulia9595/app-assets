package com.example.profile.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.EmojiEvents
import androidx.compose.material.icons.filled.MenuBook
import androidx.compose.material.icons.filled.School
import androidx.compose.material.icons.filled.SportsEsports
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.core_models.RatingHistoryItem
import java.util.Locale

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RatingHistoryScreen(
    profileViewModel: ProfileViewModel = viewModel(),
    onBack: () -> Unit
) {
    val state by profileViewModel.state.collectAsState()
    val listState = rememberLazyListState()
    val shouldLoadMore by remember {
        derivedStateOf {
            val lastVisible = listState.layoutInfo.visibleItemsInfo.lastOrNull()?.index ?: 0
            val total = listState.layoutInfo.totalItemsCount
            lastVisible >= total - 3
                    && !state.ratingHistoryLoading
                    && state.ratingHistoryHasMore
        }
    }

    LaunchedEffect(shouldLoadMore) {
        if (shouldLoadMore) profileViewModel.loadMoreRatingHistory()
    }

    LaunchedEffect(Unit) {
        profileViewModel.loadRatingHistory(reset = true)
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("История ELO") },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(Icons.Default.ArrowBack, contentDescription = "Назад")
                    }
                }
            )
        }
    ) { padding ->
        Box(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            when {
                state.ratingHistoryLoading && state.ratingHistory.isEmpty() -> {
                    CircularProgressIndicator(modifier = Modifier.align(Alignment.Center))
                }

                state.ratingHistory.isEmpty() -> {
                    Text(
                        text = "История изменений ELO пока пуста",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        modifier = Modifier.align(Alignment.Center)
                    )
                }

                else -> {
                    LazyColumn(
                        state = listState,
                        contentPadding = PaddingValues(horizontal = 16.dp, vertical = 12.dp),
                        verticalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        items(
                            items = state.ratingHistory,
                            key = { it.id }
                        ) { item ->
                            RatingHistoryCard(item)
                        }

                        if (state.ratingHistoryLoading) {
                            item {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(16.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    CircularProgressIndicator(modifier = Modifier.size(24.dp))
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
private fun RatingHistoryCard(item: RatingHistoryItem) {
    val isPositive = item.delta >= 0
    val deltaColor = if (isPositive)
        MaterialTheme.colorScheme.primary
    else
        Color(0xFFC62828)
    val deltaText = if (isPositive) "+${item.delta}" else "${item.delta}"

    val eventInfo = resolveEventInfo(item)

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.surfaceVariant.copy(alpha = 0.5f)
        )
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Box(
                modifier = Modifier
                    .size(40.dp)
                    .clip(CircleShape)
                    .background(deltaColor.copy(alpha = 0.12f)),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = eventInfo.icon,
                    contentDescription = null,
                    tint = deltaColor,
                    modifier = Modifier.size(20.dp)
                )
            }

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = eventInfo.typeLabel,
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
                if (eventInfo.name.isNotEmpty()) {
                    Text(
                        text = eventInfo.name,
                        style = MaterialTheme.typography.bodyMedium,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                }
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = "${item.oldRating} -> ${item.newRating}  •  ${formatDate(item.createdAt)}",
                    style = MaterialTheme.typography.labelSmall,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            Text(
                text = deltaText,
                style = MaterialTheme.typography.titleMedium,
                fontWeight = FontWeight.Bold,
                color = deltaColor
            )
        }
    }
}

private data class EventInfo(
    val icon: ImageVector,
    val typeLabel: String,
    val name: String
)

private fun resolveEventInfo(item: RatingHistoryItem): EventInfo {
    val reason = item.reason.lowercase()

    return when {
        item.tournamentId != null -> {
            val outcomeLabel = when {
                reason.contains("win") -> "Турнир — победа"
                reason.contains("loss") || reason.contains("lose") -> "Турнир — поражение"
                reason.contains("draw") -> "Турнир — ничья"
                else -> "Турнир"
            }
            EventInfo(
                icon = Icons.Default.SportsEsports,
                typeLabel = outcomeLabel,
                name = item.topicName.orEmpty()
            )
        }

        item.taskName != null -> {
            EventInfo(
                icon = Icons.Default.School,
                typeLabel = "Задание",
                name = item.taskName.orEmpty()
            )
        }

        item.topicName != null -> {
            EventInfo(
                icon = Icons.Default.MenuBook,
                typeLabel = "Тема пройдена",
                name = item.topicName.orEmpty()
            )
        }

        reason.contains("initial") || reason.contains("start") || reason.contains("начал") -> {
            EventInfo(
                icon = Icons.Default.EmojiEvents,
                typeLabel = "Начальный рейтинг",
                name = ""
            )
        }

        else -> {
            EventInfo(
                icon = Icons.Default.EmojiEvents,
                typeLabel = item.reason.replaceFirstChar { it.uppercase() },
                name = ""
            )
        }
    }
}

private fun formatDate(isoDate: String): String = try {
    val normalized = isoDate
        .replace(Regex("\\+\\d{2}:\\d{2}$"), "")
        .replace("Z", "")
        .replace("T", " ")
        .trimEnd()
    val sdf = java.text.SimpleDateFormat("yyyy-MM-dd HH:mm:ss", Locale("ru"))
    val date = sdf.parse(normalized)
    java.text.SimpleDateFormat("d MMM, HH:mm", Locale("ru")).format(date!!)
} catch (e: Exception) {
    isoDate.take(10)
}