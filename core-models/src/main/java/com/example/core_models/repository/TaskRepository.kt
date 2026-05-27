package com.example.core_models.repository

import com.example.core_models.dto.RunResponseDto
import com.example.core_models.dto.SubmitResponseDto
import com.example.core_models.dto.TaskDetailDto

interface TaskRepository {
    suspend fun getTask(taskId: Int): Result<TaskDetailDto>
    suspend fun runCode(taskId: Int, code: String, stdin: String? = null): Result<RunResponseDto>
    suspend fun submitSolution(taskId: Int, code: String): Result<SubmitResponseDto>
}