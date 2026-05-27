package com.example.learning

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.core_models.LevelDetailState
import com.example.core_models.LevelsState
import com.example.core_models.TopicsState
import com.example.core_models.repository.LearningRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

class LearningViewModel(
    private val repository: LearningRepository
) : ViewModel() {

    private val _topicsState = MutableStateFlow<TopicsState>(TopicsState.Idle)
    val topicsState: StateFlow<TopicsState> = _topicsState

    private val _levelsState = MutableStateFlow<LevelsState>(LevelsState.Idle)
    val levelsState: StateFlow<LevelsState> = _levelsState

    private val _levelDetailState = MutableStateFlow<LevelDetailState>(LevelDetailState.Idle)
    val levelDetailState: StateFlow<LevelDetailState> = _levelDetailState

    private val _userElo = MutableStateFlow(500)
    val userElo: StateFlow<Int> = _userElo

    private val _userName = MutableStateFlow("Игрок")
    val userName: StateFlow<String> = _userName

    fun updateUserInfo(name: String, elo: Int) {
        _userName.value = name.ifBlank { "Игрок" }
        _userElo.value = elo
    }

    fun loadTopics() {
        viewModelScope.launch {
            _topicsState.value = TopicsState.Loading
            repository.getTopics().fold(
                onSuccess = { _topicsState.value = TopicsState.Success(it) },
                onFailure = { _topicsState.value = TopicsState.Error(it.message ?: "Ошибка") }
            )
        }
    }

    fun loadLevels(topicId: Int) {
        viewModelScope.launch {
            _levelsState.value = LevelsState.Loading
            repository.getLevels(topicId).fold(
                onSuccess = { _levelsState.value = LevelsState.Success(it) },
                onFailure = { _levelsState.value = LevelsState.Error(it.message ?: "Ошибка") }
            )
        }
    }

    fun loadLevelDetail(levelId: Int) {
        viewModelScope.launch {
            _levelDetailState.value = LevelDetailState.Loading
            repository.getLevelDetail(levelId).fold(
                onSuccess = { _levelDetailState.value = LevelDetailState.Success(it) },
                onFailure = { _levelDetailState.value = LevelDetailState.Error(it.message ?: "Ошибка") }
            )
        }
    }

    fun resetLevels() { _levelsState.value = LevelsState.Idle }
    fun resetDetail() { _levelDetailState.value = LevelDetailState.Idle }
}