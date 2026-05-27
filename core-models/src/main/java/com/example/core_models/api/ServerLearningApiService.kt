package com.example.core_models.api

import com.example.core_models.dto.ApiResponse
import com.example.core_models.dto.LevelDetailDto
import com.example.core_models.dto.LevelDto
import com.example.core_models.dto.TopicDto
import retrofit2.http.GET
import retrofit2.http.Path

interface ServerLearningApiService {

    @GET("api/learning/topics")
    suspend fun getTopics(): ApiResponse<List<TopicDto>>

    @GET("api/learning/topics/{topicId}/levels")
    suspend fun getLevels(
        @Path("topicId") topicId: Int
    ): ApiResponse<List<LevelDto>>

    @GET("api/learning/levels/{levelId}")
    suspend fun getLevelDetail(
        @Path("levelId") levelId: Int
    ): ApiResponse<LevelDetailDto>
}
