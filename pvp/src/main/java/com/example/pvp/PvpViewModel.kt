package com.example.pvp

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.core_data.pvp.PvpManager
import com.example.core_models.*
import com.example.core_models.dto.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.*

class PvpViewModel(private val pvpManager: PvpManager) : ViewModel() {

    val connectionState: StateFlow<PvpConnectionState> = pvpManager.connectionState
    val inviteState: StateFlow<InviteState> = pvpManager.inviteState

    private val _lobbyState = MutableStateFlow<PvpLobbyState>(PvpLobbyState.Idle)
    val lobbyState: StateFlow<PvpLobbyState> = _lobbyState.asStateFlow()

    private val _tournamentState = MutableStateFlow<TournamentState?>(null)
    val tournamentState: StateFlow<TournamentState?> = _tournamentState.asStateFlow()

    private val _resultState = MutableStateFlow<TournamentResultState?>(null)
    val resultState: StateFlow<TournamentResultState?> = _resultState.asStateFlow()

    private val _error = MutableStateFlow<String?>(null)
    val error: StateFlow<String?> = _error.asStateFlow()

    private val _navigateToResult = MutableSharedFlow<Unit>(replay = 0)
    val navigateToResult: SharedFlow<Unit> = _navigateToResult.asSharedFlow()

    private val _pendingTournamentId = MutableStateFlow<Int?>(null)
    val pendingTournamentId: StateFlow<Int?> = _pendingTournamentId.asStateFlow()

    private var timerJob: Job? = null

    private var myUserId: Int = 0

    private var tournamentActive = false

    fun setMyUserId(id: Int) { myUserId = id }

    init {
        observeManagerEvents()
    }

    fun enterLobby() {
        if (tournamentActive) return
        if (_lobbyState.value == PvpLobbyState.Searching) return
        pvpManager.cancelSearch()
        _lobbyState.value = PvpLobbyState.Searching
        pvpManager.findMatch()
    }

    fun leaveLobby() {
        if (tournamentActive) return

        if (_lobbyState.value == PvpLobbyState.Searching) return

        val invite = pvpManager.getActivePendingInvite()
        if (invite != null) {
            pvpManager.declineInvite(invite.tournamentId)
        }

        pvpManager.cancelSearch()
        _lobbyState.value = PvpLobbyState.Idle
    }

    fun cancelAndLeave() {
        if (tournamentActive) return
        val invite = pvpManager.getActivePendingInvite()
        if (invite != null) {
            pvpManager.declineInvite(invite.tournamentId)
        }
        pvpManager.cancelSearch()
        _lobbyState.value = PvpLobbyState.Idle
    }

    fun acceptInvite(tournamentId: Int) {
        pvpManager.acceptInvite(tournamentId)
        _lobbyState.value = PvpLobbyState.WaitingForOpponent
    }

    fun declineInvite(tournamentId: Int) {
        pvpManager.declineInvite(tournamentId)
    }

    fun continueSearch() {
        if (tournamentActive) return
        _lobbyState.value = PvpLobbyState.Searching
        pvpManager.cancelSearch()
        pvpManager.findMatch()
    }

    fun stopSearch() {
        _lobbyState.value = PvpLobbyState.Idle
        pvpManager.cancelSearch()
    }

    fun submitAnswer(answerOptionId: Int) {
        val state = _tournamentState.value ?: return
        val question = state.currentQuestion ?: return

        timerJob?.cancel()

        pvpManager.submitAnswer(state.tournamentId, question.id, answerOptionId)
    }

    private fun onTimerExpired() {
        val state = _tournamentState.value ?: return
        val question = state.currentQuestion ?: return

        android.util.Log.d("PvpViewModel", "Timer expired for question ${question.id}")

        val noAnswerState = AnswerState(
            selectedOptionId = -1,
            isCorrect        = false,
            correctOptionId  = -1,
            pointsEarned     = 0
        )
        val updatedAnswers = state.myAnswers + (question.id to noAnswerState)

        if (state.isLastQuestion) {

            _tournamentState.value = state.copy(
                myAnswers = updatedAnswers,
                isFinished = true,
                timerSeconds = 0
            )

            viewModelScope.launch {

                delay(15_000)

                val currentState = _tournamentState.value

                if (
                    _resultState.value == null &&
                    currentState?.isFinished == true
                ) {

                    android.util.Log.e(
                        "PvpViewModel",
                        "Tournament force-finished locally"
                    )

                    _resultState.value = TournamentResultState(
                        tournamentId = currentState.tournamentId,
                        myUserId = myUserId,

                        result = TournamentFinishedDto(
                            tournamentId = currentState.tournamentId,
                            result = "draw",

                            scores = ScoresDto(
                                player1 = PlayerScoreDto(
                                    userId = myUserId,
                                    score = currentState.myTotalScore,
                                    ratingDelta = 0
                                ),

                                player2 = PlayerScoreDto(
                                    userId = currentState.opponentId,
                                    score = 0,
                                    ratingDelta = 0
                                )
                            )
                        ),

                        myScore = currentState.myTotalScore,
                        opponentScore = 0,

                        myRatingDelta = 0,
                        opponentRatingDelta = 0,

                        outcome = MatchOutcome.DRAW
                    )

                    tournamentActive = false

                    _navigateToResult.emit(Unit)
                }
            }

        } else {

            _tournamentState.value = state.copy(
                myAnswers = updatedAnswers,
                currentQuestionIndex = state.currentQuestionIndex + 1,
                timerSeconds = 45
            )

            startTimer()
        }
    }

    private fun moveToNextQuestion(
        answerState: AnswerState,
        questionId: Int
    ) {

        val state = _tournamentState.value ?: return

        val updatedAnswers =
            state.myAnswers + (questionId to answerState)

        val newScore =
            state.myTotalScore + answerState.pointsEarned

        if (state.isLastQuestion) {

            _tournamentState.value = state.copy(
                myAnswers = updatedAnswers,
                myTotalScore = newScore,
                isFinished = true,
                timerSeconds = 0
            )
        } else {

            _tournamentState.value = state.copy(
                myAnswers = updatedAnswers,
                myTotalScore = newScore,
                currentQuestionIndex = state.currentQuestionIndex + 1,
                timerSeconds = 45
            )

            startTimer()
        }
    }

    private fun startTimer() {

        timerJob?.cancel()

        timerJob = viewModelScope.launch {

            repeat(45) { elapsed ->

                delay(1000)

                val remaining = 44 - elapsed

                _tournamentState.update {
                    it?.copy(timerSeconds = remaining)
                }

                if (remaining <= 0) {

                    onTimerExpired()

                    return@launch
                }
            }
        }
    }

    fun clearResult() {
        tournamentActive = false
        _pendingTournamentId.value = null
        _resultState.value = null
        _tournamentState.value = null
        _lobbyState.value = PvpLobbyState.Idle
    }

    fun clearPendingNavigation() {
        _pendingTournamentId.value = null
    }

    fun clearError() { _error.value = null }

    private fun observeManagerEvents() {
        // Турнир начался
        viewModelScope.launch {
            pvpManager.tournamentStarted.collect { dto ->

                android.util.Log.d(
                    "PvpViewModel",
                    "tournamentStarted COLLECTED - tournamentId=${dto.tournamentId}"
                )

                tournamentActive = true

                timerJob?.cancel()

                _tournamentState.value = TournamentState(
                    tournamentId         = dto.tournamentId,
                    questions            = dto.questions,
                    currentQuestionIndex = 0,
                    timerSeconds         = 45
                )

                _lobbyState.value = PvpLobbyState.Idle

                _pendingTournamentId.value = dto.tournamentId

                startTimer()
            }
        }

        viewModelScope.launch {
            pvpManager.opponentInfo.collect { dto ->
                android.util.Log.d("PvpViewModel", "👤 opponentInfo COLLECTED - opponentId=${dto.opponentId}, name=${dto.opponentName}, rating=${dto.opponentRating}")
                _tournamentState.update { state ->
                    state?.copy(
                        opponentId     = dto.opponentId,
                        opponentName   = dto.opponentName,
                        opponentRating = dto.opponentRating
                    )
                }
            }
        }

        viewModelScope.launch {
            pvpManager.answerResult.collect { dto ->
                android.util.Log.d("PvpViewModel", "answerResult COLLECTED - questionId=${dto.questionId}, isCorrect=${dto.isCorrect}, points=${dto.points}")
                val state = _tournamentState.value ?: run {
                    android.util.Log.e("PvpViewModel", "tournamentState is NULL, ignoring answerResult")
                    return@collect
                }
                val answerState = AnswerState(
                    selectedOptionId = state.currentQuestion?.options
                        ?.firstOrNull { it.id != dto.correctOptionId }?.id ?: dto.correctOptionId,
                    isCorrect        = dto.isCorrect,
                    correctOptionId  = dto.correctOptionId,
                    pointsEarned     = dto.points
                )
                moveToNextQuestion(answerState, dto.questionId)
            }
        }

        viewModelScope.launch {
            pvpManager.tournamentFinished.collect { dto ->
                android.util.Log.d("PvpViewModel", "tournamentFinished COLLECTED - tournamentId=${dto.tournamentId}, result=${dto.result}")
                timerJob?.cancel()
                val p1 = dto.scores.player1
                val p2 = dto.scores.player2
                android.util.Log.d("PvpViewModel", "Player1: userId=${p1.userId}, score=${p1.score}, delta=${p1.ratingDelta}")
                android.util.Log.d("PvpViewModel", "Player2: userId=${p2.userId}, score=${p2.score}, delta=${p2.ratingDelta}")
                android.util.Log.d("PvpViewModel", "myUserId=$myUserId")

                val myScore = if (p1.userId == myUserId) p1.score else p2.score
                val oppScore = if (p1.userId == myUserId) p2.score else p1.score
                val myDelta = if (p1.userId == myUserId) p1.ratingDelta else p2.ratingDelta
                val oppDelta = if (p1.userId == myUserId) p2.ratingDelta else p1.ratingDelta

                val outcome = when {
                    dto.result == "draw" -> MatchOutcome.DRAW
                    (dto.result == "win_p1" && p1.userId == myUserId) ||
                            (dto.result == "win_p2" && p2.userId == myUserId) -> MatchOutcome.WIN
                    else -> MatchOutcome.LOSE
                }

                android.util.Log.d("PvpViewModel", "MyScore=$myScore, OppScore=$oppScore, MyDelta=$myDelta, Outcome=$outcome")

                _resultState.value = TournamentResultState(
                    tournamentId       = dto.tournamentId,
                    myUserId           = myUserId,
                    result             = dto,
                    myScore            = myScore,
                    opponentScore      = oppScore,
                    myRatingDelta      = myDelta,
                    opponentRatingDelta = oppDelta,
                    outcome            = outcome
                )
                android.util.Log.d("PvpViewModel", "Emitting navigateToResult")
                _navigateToResult.emit(Unit)
            }
        }

        viewModelScope.launch {
            pvpManager.tournamentCancelled.collect { dto ->
                tournamentActive = false
                android.util.Log.d("PvpViewModel", "tournamentCancelled COLLECTED - tournamentId=${dto.tournamentId}, reason=${dto.reason}")
                timerJob?.cancel()
                _error.value = dto.reason
                _tournamentState.value = null
                _lobbyState.value = PvpLobbyState.Idle
            }
        }

        viewModelScope.launch {
            pvpManager.serverError.collect { message ->
                android.util.Log.e("PvpViewModel", "serverError COLLECTED - message=$message")
                if (message.contains("отклонил") && !tournamentActive) {
                    _lobbyState.value = PvpLobbyState.InviteExpired("Соперник отклонил приглашение")
                } else {
                    _error.value = message
                }
            }
        }

        viewModelScope.launch {
            pvpManager.inviteExpired.collect {
                android.util.Log.d("PvpViewModel", "inviteExpired — showing dialog")
                if (!tournamentActive) {
                    _lobbyState.value = PvpLobbyState.InviteExpired("Время приглашения истекло")
                }
            }
        }
    }

    override fun onCleared() {
        android.util.Log.d("PvpViewModel", "onCleared() - cleaning up")
        timerJob?.cancel()
        super.onCleared()
    }
}