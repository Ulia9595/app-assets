package com.example.core_models

import com.example.core_models.dto.LevelDetailDto
import com.example.core_models.dto.LevelDto
import com.example.core_models.dto.TopicDto

sealed class TopicsState {
    object Idle    : TopicsState()
    object Loading : TopicsState()
    data class Success(val topics: List<TopicDto>) : TopicsState()
    data class Error(val message: String)          : TopicsState()
}

sealed class LevelsState {
    object Idle    : LevelsState()
    object Loading : LevelsState()
    data class Success(val levels: List<LevelDto>) : LevelsState()
    data class Error(val message: String)          : LevelsState()
}

sealed class LevelDetailState {
    object Idle    : LevelDetailState()
    object Loading : LevelDetailState()
    data class Success(val detail: LevelDetailDto) : LevelDetailState()
    data class Error(val message: String)          : LevelDetailState()
}