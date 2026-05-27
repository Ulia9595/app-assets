package com.example.core_models.dto

data class TaskDetailDto(
    val id: Int,
    val levelId: Int,
    val name: String,
    val condition: String,
    val difficulty: String,
    val checkType: String,
    val isSolved: Boolean = false,
    val publicTestCases: List<PublicTestCaseDto> = emptyList(),
    val hints: List<HintDto> = emptyList()
)

data class PublicTestCaseDto(
    val id: Int,
    val inputData: String?,
    val expectedOutput: String
)

data class HintDto(
    val id: Int,
    val hintText: String,
    val displayOrder: Int
)

data class RunCodeRequest(
    val code: String,
    val stdin: String? = null
)

data class RunResponseDto(
    val stdout: String,
    val stderr: String,
    val compileOutput: String,
    val exitCode: Int,
    val isSuccess: Boolean
)

data class SubmitResponseDto(
    val isCorrect: Boolean,
    val message: String,
    val testResults: List<TestResultDto>,
    val eloGained: Int,
    val passedCount: Int,
    val totalCount: Int
)

data class TestResultDto(
    val testCaseId: Int,
    val passed: Boolean,
    val inputData: String?,
    val expectedOutput: String?,
    val actualOutput: String?,
    val isHidden: Boolean,
    val errorMessage: String?
)