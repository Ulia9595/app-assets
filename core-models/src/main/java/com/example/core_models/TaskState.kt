package com.example.core_models

import com.example.core_models.dto.RunResponseDto
import com.example.core_models.dto.SubmitResponseDto
import com.example.core_models.dto.TaskDetailDto

sealed class TaskLoadState {
    object Idle    : TaskLoadState()
    object Loading : TaskLoadState()
    data class Success(val task: TaskDetailDto) : TaskLoadState()
    data class Error(val message: String)       : TaskLoadState()
}

sealed class RunState {
    object Idle      : RunState()
    object Running   : RunState()
    data class Success(val result: RunResponseDto) : RunState()
    data class Error(val message: String)          : RunState()
}

sealed class SubmitState {
    object Idle        : SubmitState()
    object Submitting  : SubmitState()
    data class Success(val result: SubmitResponseDto) : SubmitState()
    data class Error(val message: String)             : SubmitState()
}