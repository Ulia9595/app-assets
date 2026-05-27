package com.example.core_data.pvp

import com.example.core_models.InviteState
import com.example.core_models.PvpConnectionState
import com.example.core_models.dto.*
import com.google.gson.Gson
import com.microsoft.signalr.HubConnection
import com.microsoft.signalr.HubConnectionBuilder
import com.microsoft.signalr.HubConnectionState
import io.reactivex.rxjava3.core.Single
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow

class PvpManager(
    private val serverUrl: String,
    private val tokenProvider: () -> String?
) {
    private val scope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private val gson = Gson()

    private val _connectionState = MutableStateFlow<PvpConnectionState>(PvpConnectionState.Disconnected)
    val connectionState: StateFlow<PvpConnectionState> = _connectionState.asStateFlow()

    private val _inviteState = MutableStateFlow<InviteState>(InviteState.None)
    val inviteState: StateFlow<InviteState> = _inviteState.asStateFlow()

    private val _tournamentStarted = MutableSharedFlow<TournamentStartedDto>(replay = 0)
    val tournamentStarted: SharedFlow<TournamentStartedDto> = _tournamentStarted.asSharedFlow()

    private val _opponentInfo = MutableSharedFlow<OpponentInfoDto>(replay = 1)
    val opponentInfo: SharedFlow<OpponentInfoDto> = _opponentInfo.asSharedFlow()

    private val _answerResult = MutableSharedFlow<AnswerResultDto>(replay = 0)
    val answerResult: SharedFlow<AnswerResultDto> = _answerResult.asSharedFlow()

    private val _tournamentFinished = MutableSharedFlow<TournamentFinishedDto>(replay = 0)
    val tournamentFinished: SharedFlow<TournamentFinishedDto> = _tournamentFinished.asSharedFlow()

    private val _tournamentCancelled = MutableSharedFlow<TournamentCancelledDto>(replay = 0)
    val tournamentCancelled: SharedFlow<TournamentCancelledDto> = _tournamentCancelled.asSharedFlow()

    private val _serverError = MutableSharedFlow<String>(replay = 0)
    val serverError: SharedFlow<String> = _serverError.asSharedFlow()

    private val _inviteExpired = MutableSharedFlow<Unit>(replay = 0)
    val inviteExpired: SharedFlow<Unit> = _inviteExpired.asSharedFlow()

    private val refuseCount = mutableMapOf<Int, Int>()
    private val blockedUntil = mutableMapOf<Int, Long>()
    private val blockDurationMs = 60 * 60 * 1000L
    private val maxRefusals = 3

    private var inviteExpiryJob: Job? = null

    private var hub: HubConnection? = null

    fun connect() {
        android.util.Log.d("PvpManager", "connect() called")
        if (_connectionState.value == PvpConnectionState.Connected ||
            _connectionState.value == PvpConnectionState.Connecting) {
            android.util.Log.d("PvpManager", "Already connected or connecting, skipping")
            return
        }

        val token = tokenProvider()
        android.util.Log.d("PvpManager", "token = ${token?.take(20)}...")

        if (token == null) {
            android.util.Log.e("PvpManager", "token is null — aborting connect")
            return
        }

        _connectionState.value = PvpConnectionState.Connecting

        hub = HubConnectionBuilder
            .create("$serverUrl/hubs/tournament")
            .withAccessTokenProvider(Single.defer { Single.just(token) })
            .build()

        registerHandlers()

        android.util.Log.d("PvpManager", "Starting SignalR connection...")
        hub!!.start()
            .doOnComplete {
                android.util.Log.d("PvpManager", "SignalR connected successfully!")
                scope.launch { _connectionState.emit(PvpConnectionState.Connected) }
            }
            .doOnError { err ->
                android.util.Log.e("PvpManager", "SignalR FAILED: ${err::class.simpleName}: ${err.message}")
                err.cause?.let { android.util.Log.e("PvpManager", "Cause: ${it.message}") }
                scope.launch {
                    _connectionState.emit(PvpConnectionState.Error(err.message ?: "Ошибка подключения"))
                }
            }
            .subscribe({}, { err ->
                android.util.Log.e("PvpManager", "subscribe error: ${err.message}")
            })

        hub!!.onClosed { err ->
            android.util.Log.d("PvpManager", "SignalR closed: ${err?.message ?: "normal"}")
            scope.launch {
                _connectionState.emit(
                    if (err != null) PvpConnectionState.Error(err.message ?: "Соединение разорвано")
                    else PvpConnectionState.Disconnected
                )
            }
        }
    }

    fun disconnect() {
        android.util.Log.d("PvpManager", "disconnect() called")
        inviteExpiryJob?.cancel()
        _inviteState.value = InviteState.None
        hub?.stop()
        hub = null
        _connectionState.value = PvpConnectionState.Disconnected
        refuseCount.clear()
        blockedUntil.clear()
    }

    fun getActivePendingInvite(): InviteReceivedDto? {
        return when (val s = _inviteState.value) {
            is InviteState.Pending          -> s.invite
            is InviteState.OpponentAccepted -> s.invite
            else                            -> null
        }
    }

    val isConnected: Boolean
        get() = hub?.connectionState == HubConnectionState.CONNECTED

    fun findMatch(topicId: Int = 0) {
        android.util.Log.d("PvpManager", "findMatch() called with topicId=$topicId")
        invokeHub("FindMatch", topicId)
    }

    fun cancelSearch() {
        android.util.Log.d("PvpManager", "cancelSearch() called")
        invokeHub("CancelSearch")
    }

    fun acceptInvite(tournamentId: Int) {
        android.util.Log.d("PvpManager", "acceptInvite() called for tournamentId=$tournamentId")

        if (_inviteState.value is InviteState.AcceptedWaiting) {
            android.util.Log.w("PvpManager", "Already in AcceptedWaiting, ignoring duplicate accept")
            return
        }

        val invite = when (val s = _inviteState.value) {
            is InviteState.Pending         -> s.invite
            is InviteState.OpponentAccepted -> s.invite
            else -> {
                android.util.Log.e("PvpManager", "No active invite to accept!")
                return
            }
        }

        _inviteState.value = InviteState.AcceptedWaiting(invite)

        inviteExpiryJob?.cancel()
        inviteExpiryJob = null

        invokeHub("AcceptInvite", tournamentId)
    }

    fun declineInvite(tournamentId: Int) {
        android.util.Log.d("PvpManager", "declineInvite() called for tournamentId=$tournamentId")

        if (_inviteState.value is InviteState.AcceptedWaiting) {
            android.util.Log.w("PvpManager", "Already accepted, can't decline")
            return
        }

        val invite = when (val s = _inviteState.value) {
            is InviteState.Pending          -> s.invite
            is InviteState.OpponentAccepted -> s.invite
            else -> {
                android.util.Log.e("PvpManager", "No pending invite found!")
                return
            }
        }
        val opponentId = invite.opponentId
        clearInvite()

        val count = (refuseCount[opponentId] ?: 0) + 1
        refuseCount[opponentId] = count
        android.util.Log.d("PvpManager", "Refusal count for opponent $opponentId: $count")

        if (count >= maxRefusals) {
            blockedUntil[opponentId] = System.currentTimeMillis() + blockDurationMs
            android.util.Log.d("PvpManager", "Opponent $opponentId blocked for 1 hour")
        }

        invokeHub("DeclineInvite", tournamentId)
    }

    fun submitAnswer(tournamentId: Int, questionId: Int, answerOptionId: Int) {
        android.util.Log.d("PvpManager", "submitAnswer() called: tournamentId=$tournamentId, questionId=$questionId, answerOptionId=$answerOptionId")
        invokeHub("SubmitAnswer", tournamentId, questionId, answerOptionId)
    }

    private fun registerHandlers() {
        val h = hub ?: run {
            android.util.Log.e("PvpManager", "registerHandlers: hub is null!")
            return
        }

        android.util.Log.d("PvpManager", "Registering SignalR handlers...")

        fun <T> parseEvent(name: String, raw: Any, clazz: Class<T>): T? {
            return try {
                val json = gson.toJson(raw)
                android.util.Log.d("PvpManager", "   [$name] raw JSON: $json")
                gson.fromJson(json, clazz)
            } catch (e: Exception) {
                android.util.Log.e("PvpManager", "   [$name] parse error: ${e.message}", e)
                null
            }
        }

        h.on("InviteAcceptedByMe", { raw ->
            android.util.Log.d("PvpManager", "InviteAcceptedByMe triggered")
            val tournamentId = try { (raw as? Double)?.toInt() ?: raw as Int } catch(e: Exception) { 0 }

            val current = _inviteState.value
            if (current is InviteState.Pending && current.invite.tournamentId == tournamentId) {
                _inviteState.value = InviteState.AcceptedWaiting(current.invite)
            }
        }, Object::class.java)

        h.on("OpponentAccepted", { raw ->
            android.util.Log.d("PvpManager", "OpponentAccepted triggered")
            parseEvent("OpponentAccepted", raw, OpponentAcceptedDto::class.java)?.let { data ->
                val current = _inviteState.value
                if (current is InviteState.Pending && current.invite.tournamentId == data.tournamentId) {
                    _inviteState.value = InviteState.OpponentAccepted(current.invite, data.opponentName)
                }
            }
        }, Object::class.java)

        h.on("InviteReceived", { raw ->
            android.util.Log.d("PvpManager", "INVITE_RECEIVED triggered!")
            parseEvent("InviteReceived", raw, InviteReceivedDto::class.java)?.let {
                android.util.Log.d("PvpManager", "tournamentId=${it.tournamentId}, opponent=${it.opponentName}")
                handleInviteReceived(it)
            }
        }, Object::class.java)

        h.on("TournamentStarted", { raw ->
            android.util.Log.d("PvpManager", "TOURNAMENT_STARTED triggered!")
            parseEvent("TournamentStarted", raw, TournamentStartedDto::class.java)?.let {
                android.util.Log.d("PvpManager", "tournamentId=${it.tournamentId}, questions=${it.questions.size}")
                clearInvite()
                scope.launch { _tournamentStarted.emit(it) }
            }
        }, Object::class.java)

        h.on("OpponentInfo", { raw ->
            android.util.Log.d("PvpManager", "OPPONENT_INFO triggered!")
            parseEvent("OpponentInfo", raw, OpponentInfoDto::class.java)?.let {
                android.util.Log.d("PvpManager", "opponentId=${it.opponentId}, rating=${it.opponentRating}")
                scope.launch { _opponentInfo.emit(it) }
            }
        }, Object::class.java)

        h.on("AnswerResult", { raw ->
            android.util.Log.d("PvpManager", "ANSWER_RESULT triggered!")
            parseEvent("AnswerResult", raw, AnswerResultDto::class.java)?.let {
                android.util.Log.d("PvpManager", "questionId=${it.questionId}, isCorrect=${it.isCorrect}, points=${it.points}")
                scope.launch { _answerResult.emit(it) }
            }
        }, Object::class.java)

        h.on("TournamentFinished", { raw ->
            android.util.Log.d("PvpManager", "TOURNAMENT_FINISHED triggered!")
            parseEvent("TournamentFinished", raw, TournamentFinishedDto::class.java)?.let {
                android.util.Log.d("PvpManager", "tournamentId=${it.tournamentId}, result=${it.result}")
                scope.launch { _tournamentFinished.emit(it) }
            }
        }, Object::class.java)

        h.on("TournamentCancelled", { raw ->
            android.util.Log.d("PvpManager", "TOURNAMENT_CANCELLED triggered!")
            parseEvent("TournamentCancelled", raw, TournamentCancelledDto::class.java)?.let {
                android.util.Log.d("PvpManager", "tournamentId=${it.tournamentId}, reason=${it.reason}")
                scope.launch { _tournamentCancelled.emit(it) }
            }
        }, Object::class.java)

        h.on("WaitingForOpponent", { raw ->
            android.util.Log.d("PvpManager", "WAITING_FOR_OPPONENT triggered!")
        }, Object::class.java)

        h.on("InviteDeclined", { raw ->
            android.util.Log.d("PvpManager", "INVITE_DECLINED triggered!")
            clearInvite()
            scope.launch { _serverError.emit("Соперник отклонил приглашение") }
        }, Object::class.java)

        h.on("InviteExpired", { raw ->
            android.util.Log.d("PvpManager", "INVITE_EXPIRED triggered!")
            clearInvite()
            scope.launch { _inviteExpired.emit(Unit) }
        }, Object::class.java)

        h.on("Searching", { raw ->
            android.util.Log.d("PvpManager", "SEARCHING triggered!")
        }, Object::class.java)

        h.on("SearchCancelled", {
            android.util.Log.d("PvpManager", "SEARCH_CANCELLED triggered!")
        })

        h.on("Error", { raw ->
            val message = raw.toString()
            android.util.Log.e("PvpManager", "SERVER ERROR: $message")
            scope.launch { _serverError.emit(message) }
        }, Object::class.java)

        android.util.Log.d("PvpManager", "All handlers registered")
    }

    private fun handleInviteReceived(dto: InviteReceivedDto) {
        if (_inviteState.value !is InviteState.None) {
            android.util.Log.d("PvpManager", "Busy with another invite/game, auto-declining")
            invokeHub("DeclineInvite", dto.tournamentId)
            return
        }
        android.util.Log.d("PvpManager", "handleInviteReceived: processing invite for tournament ${dto.tournamentId}")

        val blockEnd = blockedUntil[dto.opponentId]
        if (blockEnd != null) {
            if (System.currentTimeMillis() < blockEnd) {
                android.util.Log.d("PvpManager", "Opponent ${dto.opponentId} is blocked, auto-declining")
                invokeHub("DeclineInvite", dto.tournamentId)
                return
            } else {
                android.util.Log.d("PvpManager", "Block expired for opponent ${dto.opponentId}")
                blockedUntil.remove(dto.opponentId)
                refuseCount.remove(dto.opponentId)
            }
        }

        android.util.Log.d("PvpManager", "Setting inviteState to Pending for tournament ${dto.tournamentId}")
        _inviteState.value = InviteState.Pending(dto)

        inviteExpiryJob?.cancel()
        inviteExpiryJob = scope.launch {
            android.util.Log.d("PvpManager", "Starting invite expiry timer for ${dto.expiresInSeconds} seconds")
            delay(dto.expiresInSeconds * 1000L)
            val currentState = _inviteState.value
            if (currentState is InviteState.Pending || currentState is InviteState.OpponentAccepted) {
                android.util.Log.d("PvpManager", "Invite expired for tournament ${dto.tournamentId}, auto-declining")
                clearInvite()
                invokeHub("DeclineInvite", dto.tournamentId)
            } else if (currentState is InviteState.AcceptedWaiting) {
                android.util.Log.d("PvpManager", "Invite expired but we already accepted, just clearing UI")
                clearInvite()
            }
        }
    }

    private fun clearInvite() {
        android.util.Log.d("PvpManager", "clearInvite() called")
        inviteExpiryJob?.cancel()
        _inviteState.value = InviteState.None
    }

    private fun invokeHub(method: String, vararg args: Any) {
        val h = hub ?: run {
            android.util.Log.e("PvpManager", "invokeHub: hub is null, can't call $method")
            return
        }
        if (h.connectionState != HubConnectionState.CONNECTED) {
            android.util.Log.e("PvpManager", "invokeHub: not connected (state=${h.connectionState}), can't call $method")
            return
        }
        android.util.Log.d("PvpManager", "Invoking hub method '$method' with ${args.size} args")
        try {
            when (args.size) {
                0 -> h.invoke(method)
                1 -> h.invoke(method, args[0])
                2 -> h.invoke(method, args[0], args[1])
                3 -> h.invoke(method, args[0], args[1], args[2])
            }
            android.util.Log.d("PvpManager", "Invoke successful for '$method'")
        } catch (e: Exception) {
            android.util.Log.e("PvpManager", "Invoke failed for '$method': ${e.message}", e)
            scope.launch { _serverError.emit("Ошибка отправки: ${e.message}") }
        }
    }
}