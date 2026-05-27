package com.example.core_models.dto

data class TopicDto(
    val id: Int,
    val name: String,
    val description: String?,
    val displayOrder: Int,
    val levelCount: Int,
    val completedLevelCount: Int = 0,
    val isCompleted: Boolean = false,
    val isLocked: Boolean = false
) {
    val topicProgress: Float
        get() = if (levelCount > 0) completedLevelCount.toFloat() / levelCount else 0f
}

data class LevelDto(
    val id: Int,
    val topicId: Int,
    val name: String,
    val levelNumber: Int,
    val hasTheory: Boolean,
    val taskCount: Int,
    val isCompleted: Boolean = false,
    val isLocked: Boolean = false,
    val tasksCompleted: Int = 0
)

data class LevelDetailDto(
    val id: Int,
    val topicId: Int,
    val name: String,
    val levelNumber: Int,
    val theory: TheoryDto?,
    val tasks: List<PracticeTaskDto>
)

data class TheoryDto(
    val id: Int,
    val title: String,
    val content: String
)

data class PracticeTaskDto(
    val id: Int,
    val name: String,
    val condition: String,
    val difficulty: String,
    val displayOrder: Int,
    val isSolved: Boolean = false
)