package com.example.learning

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.core_models.RunState
import com.example.core_models.SubmitState
import com.example.core_models.TaskLoadState
import com.example.core_models.repository.TaskRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class TaskViewModel(
    private val repository: TaskRepository
) : ViewModel() {

    private val _taskState = MutableStateFlow<TaskLoadState>(TaskLoadState.Idle)
    val taskState: StateFlow<TaskLoadState> = _taskState

    private val _runState = MutableStateFlow<RunState>(RunState.Idle)
    val runState: StateFlow<RunState> = _runState

    private val _submitState = MutableStateFlow<SubmitState>(SubmitState.Idle)
    val submitState: StateFlow<SubmitState> = _submitState

    private val _code = MutableStateFlow("")
    val code: StateFlow<String> = _code

    private val _hintsRevealed = MutableStateFlow(0)
    val hintsRevealed: StateFlow<Int> = _hintsRevealed

    fun loadTask(taskId: Int) {
        viewModelScope.launch {
            _taskState.value = TaskLoadState.Loading
            repository.getTask(taskId).fold(
                onSuccess = { _taskState.value = TaskLoadState.Success(it) },
                onFailure = { _taskState.value = TaskLoadState.Error(it.message ?: "Ошибка") }
            )
        }
    }

    fun updateCode(newCode: String) {
        _code.value = newCode
    }

    fun runCode(taskId: Int) {
        val currentCode = _code.value
        if (currentCode.isBlank()) return

        viewModelScope.launch {
            _runState.value = RunState.Running
            repository.runCode(taskId, currentCode).fold(
                onSuccess = { _runState.value = RunState.Success(it) },
                onFailure = { _runState.value = RunState.Error(it.message ?: "Ошибка выполнения") }
            )
        }
    }

    fun submitSolution(taskId: Int) {
        val currentCode = _code.value
        if (currentCode.isBlank()) return

        viewModelScope.launch {
            _submitState.value = SubmitState.Submitting
            repository.submitSolution(taskId, currentCode).fold(
                onSuccess = { _submitState.value = SubmitState.Success(it) },
                onFailure = { _submitState.value = SubmitState.Error(it.message ?: "Ошибка отправки") }
            )
        }
    }

    fun revealNextHint() {
        val task = (_taskState.value as? TaskLoadState.Success)?.task ?: return
        if (_hintsRevealed.value < task.hints.size) {
            _hintsRevealed.value++
        }
    }

    fun resetRun() { _runState.value = RunState.Idle }
    fun resetSubmit() { _submitState.value = SubmitState.Idle }

    fun reset() {
        _taskState.value = TaskLoadState.Idle
        _runState.value = RunState.Idle
        _submitState.value = SubmitState.Idle
        _code.value = ""
        _hintsRevealed.value = 0
    }
}