package com.example.core_data.repository

import com.example.core_models.api.TaskApiService
import com.example.core_models.dto.RunCodeRequest
import com.example.core_models.dto.RunResponseDto
import com.example.core_models.dto.SubmitResponseDto
import com.example.core_models.dto.TaskDetailDto
import com.example.core_models.repository.TaskRepository

class ServerTaskRepositoryImpl(
    private val api: TaskApiService
) : TaskRepository {

    override suspend fun getTask(taskId: Int): Result<TaskDetailDto> = runCatching {
        val response = api.getTask(taskId)
        response.data ?: throw Exception(response.error ?: "Задание не найдено")
    }

    override suspend fun runCode(
        taskId: Int, code: String, stdin: String?
    ): Result<RunResponseDto> = runCatching {
        val response = api.runCode(taskId, RunCodeRequest(code, stdin))
        response.data ?: throw Exception(response.error ?: "Ошибка выполнения")
    }

    override suspend fun submitSolution(
        taskId: Int, code: String
    ): Result<SubmitResponseDto> = runCatching {
        val response = api.submitSolution(taskId, RunCodeRequest(code))
        response.data ?: throw Exception(response.error ?: "Ошибка отправки")
    }
}