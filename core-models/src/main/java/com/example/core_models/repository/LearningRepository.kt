package com.example.core_models.repository

import com.example.core_models.dto.LevelDetailDto
import com.example.core_models.dto.LevelDto
import com.example.core_models.dto.TopicDto

interface LearningRepository {
    suspend fun getTopics(): Result<List<TopicDto>>
    suspend fun getLevels(topicId: Int): Result<List<LevelDto>>
    suspend fun getLevelDetail(levelId: Int): Result<LevelDetailDto>
}