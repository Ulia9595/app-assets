package com.example.learning.ui

import androidx.compose.ui.text.style.TextAlign
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.example.core.ui.UserEloHeader

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TheoryScreen(
    title: String,
    content: String,
    userName: String = "",
    userElo: Int = 0,
    onBack: () -> Unit
) {
    val scrollState = rememberScrollState()

    Scaffold(
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        text = "Теория",
                        style = MaterialTheme.typography.titleMedium
                            .copy(fontWeight = FontWeight.Bold)
                    )
                },
                navigationIcon = {
                    IconButton(onClick = onBack) {
                        Icon(
                            Icons.AutoMirrored.Filled.ArrowBack,
                            contentDescription = "Назад к заданиям"
                        )
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

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .verticalScroll(scrollState)
                    .padding(horizontal = 20.dp, vertical = 16.dp)
            ) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.headlineSmall
                        .copy(fontWeight = FontWeight.Bold),
                    color = MaterialTheme.colorScheme.onBackground
                )

                Spacer(Modifier.height(16.dp))

                HorizontalDivider(
                    color = MaterialTheme.colorScheme.outlineVariant,
                    thickness = 1.dp
                )

                Spacer(Modifier.height(16.dp))

                val blocks = parseTheoryContent(content)
                blocks.forEach { block ->
                    when (block) {
                        is TheoryBlock.Text -> {
                            Text(
                                text = block.value,
                                style = MaterialTheme.typography.bodyLarge,
                                color = MaterialTheme.colorScheme.onBackground,
                                lineHeight = MaterialTheme.typography.bodyLarge.lineHeight,
                                textAlign = TextAlign.Justify,
                                modifier = Modifier.fillMaxWidth()
                            )
                            Spacer(Modifier.height(12.dp))
                        }
                        is TheoryBlock.Code -> {
                            CodeBlock(code = block.value)
                            Spacer(Modifier.height(12.dp))
                        }
                    }
                }

                Spacer(Modifier.height(24.dp))
                Button(
                    onClick = onBack,
                    modifier = Modifier.align(Alignment.CenterHorizontally)
                ) {
                    Text("Вернуться к заданиям")
                }
                Spacer(Modifier.height(16.dp))
            }
        }
    }
}

@Composable
private fun CodeBlock(code: String) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(8.dp))
            .background(MaterialTheme.colorScheme.surfaceVariant)
            .padding(12.dp)
    ) {
        Text(
            text = code.trim(),
            style = MaterialTheme.typography.bodyMedium.copy(
                fontFamily = androidx.compose.ui.text.font.FontFamily.Monospace
            ),
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

private sealed class TheoryBlock {
    data class Text(val value: String) : TheoryBlock()
    data class Code(val value: String) : TheoryBlock()
}

private fun parseTheoryContent(content: String): List<TheoryBlock> {
    val blocks = mutableListOf<TheoryBlock>()
    val parts = content.split("```")
    parts.forEachIndexed { index, part ->
        if (part.isBlank()) return@forEachIndexed
        if (index % 2 == 0) {
            blocks.add(TheoryBlock.Text(part.trim()))
        } else {
            val codeContent = if (part.contains('\n')) {
                part.substringAfter('\n')
            } else {
                part
            }
            blocks.add(TheoryBlock.Code(codeContent))
        }
    }
    return blocks.ifEmpty { listOf(TheoryBlock.Text(content)) }
}