package com.example.core_data.repository

import com.example.core_models.api.ServerLearningApiService
import com.example.core_models.dto.LevelDetailDto
import com.example.core_models.dto.LevelDto
import com.example.core_models.dto.TopicDto
import com.example.core_models.repository.LearningRepository

class ServerLearningRepositoryImpl(
    private val api: ServerLearningApiService
) : LearningRepository {

    override suspend fun getTopics(): Result<List<TopicDto>> = runCatching {
        val response = api.getTopics()
        response.data ?: throw Exception(response.error ?: "Не удалось загрузить темы")
    }

    override suspend fun getLevels(topicId: Int): Result<List<LevelDto>> = runCatching {
        val response = api.getLevels(topicId)
        response.data ?: throw Exception(response.error ?: "Не удалось загрузить уровни")
    }

    override suspend fun getLevelDetail(levelId: Int): Result<LevelDetailDto> = runCatching {
        val response = api.getLevelDetail(levelId)
        response.data ?: throw Exception(response.error ?: "Не удалось загрузить уровень")
    }
}