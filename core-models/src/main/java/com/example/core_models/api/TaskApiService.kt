package com.example.core_models.api

import com.example.core_models.dto.ApiResponse
import com.example.core_models.dto.RunCodeRequest
import com.example.core_models.dto.RunResponseDto
import com.example.core_models.dto.SubmitResponseDto
import com.example.core_models.dto.TaskDetailDto
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface TaskApiService {

    @GET("api/tasks/{taskId}")
    suspend fun getTask(
        @Path("taskId") taskId: Int
    ): ApiResponse<TaskDetailDto>

    @POST("api/tasks/{taskId}/run")
    suspend fun runCode(
        @Path("taskId") taskId: Int,
        @Body request: RunCodeRequest
    ): ApiResponse<RunResponseDto>

    @POST("api/tasks/{taskId}/submit")
    suspend fun submitSolution(
        @Path("taskId") taskId: Int,
        @Body request: RunCodeRequest
    ): ApiResponse<SubmitResponseDto>
}