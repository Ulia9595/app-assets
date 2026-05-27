'use strict';

let pvSelTournamentId = null;
let pvSelQuestionId = null;

function pvEsc(s) {
    return String(s ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

function pvEscJs(s) {
    return String(s ?? '').replace(/\\/g, '\\\\').replace(/'/g, "\\'");
}

function pvShowToast(msg, type = 'success') {
    const t = document.getElementById('pv-toast');
    t.textContent = msg;
    t.className = 'lc-toast lc-toast--' + type + ' lc-toast--show';
    clearTimeout(t._timer);
    t._timer = setTimeout(() => t.classList.remove('lc-toast--show'), 3200);
}

function pvConfirm(text, cb) {
    document.getElementById('pv-confirm-text').textContent = text;
    const btn = document.getElementById('pv-btn-confirm');
    btn.onclick = () => { closeModal('pv-modal-confirm'); cb(); };
    openModal('pv-modal-confirm');
}

async function pvPost(url, data) {
    const fd = new FormData();
    fd.append('__RequestVerificationToken', PV_TOKEN);
    Object.entries(data).forEach(([k, v]) => { if (v !== undefined && v !== null) fd.append(k, v); });
    const res = await fetch(url, { method: 'POST', body: fd });
    const json = await res.json();
    if (!res.ok) throw new Error(json.error || 'Ошибка запроса');
    return json;
}

function pvDiffBadge(name) {
    if (name === 'Лёгкий') return 'lc-badge--green';
    if (name === 'Средний') return 'lc-badge--blue';
    if (name === 'Сложный') return 'lc-badge--red';
    return '';
}

function pvStatusBadge(name) {
    if (name === 'В процессе') return 'lc-badge--green';
    if (name === 'Завершён') return 'lc-badge--gray';
    if (name === 'Отменён') return 'lc-badge--red';
    if (name === 'Неактивен') return 'lc-badge--gray';
    return 'lc-badge--blue';
}

async function pvReload() {
    try {
        const res = await fetch('/admin/pvp', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const html = await res.text();
        const doc = new DOMParser().parseFromString(html, 'text/html');

        const scripts = doc.querySelectorAll('script');
        scripts.forEach(s => {
            const mT = s.textContent.match(/const PV_TOURNAMENTS\s*=\s*(\[[\s\S]*?\]);/);
            if (mT) { try { PV_TOURNAMENTS.length = 0; JSON.parse(mT[1]).forEach(x => PV_TOURNAMENTS.push(x)); } catch { } }

            const mQ = s.textContent.match(/const PV_QUESTIONS\s*=\s*(\[[\s\S]*?\]);/);
            if (mQ) { try { PV_QUESTIONS.length = 0; JSON.parse(mQ[1]).forEach(x => PV_QUESTIONS.push(x)); } catch { } }
        });

        const newTList = doc.getElementById('pv-tournaments-list');
        if (newTList) {
            document.getElementById('pv-tournaments-list').innerHTML = newTList.innerHTML;
            document.getElementById('pv-tournaments-count').textContent = PV_TOURNAMENTS.length + ' шт.';
        }

        if (pvSelTournamentId) {
            const t = PV_TOURNAMENTS.find(x => x.id === pvSelTournamentId);
            if (t) pvRenderTournamentQuestions(pvSelTournamentId);
            else pvLockTournamentQuestions();
        }

        if (pvSelQuestionId) {
            const q = PV_QUESTIONS.find(x => x.id === pvSelQuestionId);
            if (q) pvRenderAnswers(pvSelQuestionId);
            else pvLockAnswers();
        }

        document.getElementById('pv-questions-count').textContent = PV_QUESTIONS.length + ' шт.';
        pvRenderQuestionBank();

    } catch {
        pvShowToast('Ошибка обновления данных', 'error');
    }
}

function pvPopulateTournamentForm(t = null) {
    const topicSel = document.getElementById('pv-tournament-topic');
    const statusSel = document.getElementById('pv-tournament-status');
    const statusField = document.getElementById('pv-tournament-status-field');

    topicSel.innerHTML = PV_TOPICS.map(x =>
        `<option value="${x.id}" ${t && t.topicId === x.id ? 'selected' : ''}>${pvEsc(x.name)}</option>`
    ).join('');

    statusSel.innerHTML = PV_STATUSES.map(x =>
        `<option value="${x.id}" ${t && t.statusId === x.id ? 'selected' : ''}>${pvEsc(x.name)}</option>`
    ).join('');

    document.getElementById('pv-tournament-id').value = t ? t.id : 0;

    statusField.style.display = t ? '' : 'none';
}
function pvEditTournament(id) {
    const t = PV_TOURNAMENTS.find(x => x.id === Number(id));
    if (!t) return;
    pvPopulateTournamentForm(t);
    document.getElementById('pv-modal-tournament-title').textContent = 'Редактировать турнир';
    openModal('pv-modal-tournament');
}

async function pvSaveTournament() {
    const id = parseInt(document.getElementById('pv-tournament-id').value);
    const topicId = document.getElementById('pv-tournament-topic').value;
    const statusId = document.getElementById('pv-tournament-status').value;

    const isNew = id === 0;
    const url = isNew ? '/admin/tournaments/create' : '/admin/tournaments/update';
    const data = isNew
        ? { TopicId: topicId, StatusId: 5 }
        : { Id: id, TopicId: topicId, StatusId: statusId };

    try {
        const res = await pvPost(url, data);
        closeModal('pv-modal-tournament');
        pvShowToast(res.message);
        await pvReload();
    } catch (e) { pvShowToast(e.message, 'error'); }
}

function pvDeleteTournament(id, name) {
    pvConfirm(`Удалить турнир «${name}»? Все вопросы турнира будут откреплены.`, async () => {
        try {
            const res = await pvPost(`/admin/tournaments/delete/${id}`, {});
            pvShowToast(res.message);
            if (pvSelTournamentId === Number(id)) pvLockTournamentQuestions();
            await pvReload();
        } catch (e) { pvShowToast(e.message, 'error'); }
    });
}

function pvSelectTournament(id, name) {
    pvSelTournamentId = Number(id);
    document.getElementById('pv-tournament-label').textContent = name;
    document.getElementById('pv-col-tquestions').classList.remove('lc-col--locked');
    pvRenderTournamentQuestions(pvSelTournamentId);
    pvRenderQuestionBank();
}

function pvLockTournamentQuestions() {
    pvSelTournamentId = null;
    document.getElementById('pv-tournament-label').textContent = 'Выберите турнир';
    document.getElementById('pv-col-tquestions').classList.add('lc-col--locked');
    document.getElementById('pv-tquestions-list').innerHTML = `<div class="lc-empty-state">
        <i class="fa fa-list-ol"></i><p>Выберите турнир слева</p></div>`;
}

function pvRenderTournamentQuestions(tournamentId) {
    const t = PV_TOURNAMENTS.find(x => x.id === tournamentId);
    const list = document.getElementById('pv-tquestions-list');

    if (!t) { pvLockTournamentQuestions(); return; }

    if (t.questions.length === 0) {
        list.innerHTML = `<div class="lc-empty-state">
            <i class="fa fa-list-ol"></i><p>В турнире ещё нет вопросов.<br>Добавьте из банка вопросов</p></div>`;
        return;
    }

    list.innerHTML = t.questions.map(q => `
        <div class="lc-item">
            <div class="lc-item-info">
                <span class="lc-item-order">№${q.questionNumber}</span>
                <span class="lc-item-name">${pvEsc(q.questionText.length > 70 ? q.questionText.substring(0, 70) + '…' : q.questionText)}</span>
            </div>
            <div class="lc-item-meta">
                <span class="lc-badge ${pvDiffBadge(q.difficultyTypeName)}">${pvEsc(q.difficultyTypeName)}</span>
            </div>
            <div class="lc-item-actions">
                <button class="lc-icon-btn lc-delete" title="Убрать из турнира"
                        onclick="pvRemoveTournamentQuestion(${tournamentId}, ${q.questionId})">
                    <i class="fa fa-times"></i>
                </button>
            </div>
        </div>`).join('');
}

function pvRemoveTournamentQuestion(tournamentId, questionId) {
    pvConfirm('Убрать вопрос из турнира?', async () => {
        try {
            const res = await pvPost('/admin/tournaments/questions/remove', {
                TournamentId: tournamentId,
                QuestionId: questionId
            });
            pvShowToast(res.message);
            await pvReload();
        } catch (e) { pvShowToast(e.message, 'error'); }
    });
}

function pvRenderQuestionBank() {
    const list = document.getElementById('pv-questions-list');
    if (PV_QUESTIONS.length === 0) {
        list.innerHTML = `<div class="lc-empty-state">
            <i class="fa fa-question-circle"></i><p>Вопросов пока нет</p></div>`;
        return;
    }
    const tournament = pvSelTournamentId
        ? PV_TOURNAMENTS.find(x => x.id === pvSelTournamentId)
        : null;
    const addedIds = new Set(tournament ? tournament.questions.map(q => q.questionId) : []);

    list.innerHTML = PV_QUESTIONS.map(q => {
        const alreadyAdded = addedIds.has(q.id);
        const wrongTopic = tournament && q.topicId !== tournament.topicId;
        const diffClass = q.difficultyTypeName === 'Лёгкий' ? 'lc-badge--green'
            : q.difficultyTypeName === 'Сложный' ? 'lc-badge--red' : 'lc-badge--blue';
        return `
        <div class="lc-item" data-id="${q.id}">
            <div class="lc-item-info">
                <span class="lc-item-order">${pvEsc(q.topicName)}</span>
                <span class="lc-item-name">${pvEsc(q.questionText.length > 60 ? q.questionText.substring(0, 60) + '…' : q.questionText)}</span>
            </div>
            <div class="lc-item-meta">
                <span class="lc-badge ${diffClass}">${pvEsc(q.difficultyTypeName)}</span>
                <span class="lc-badge lc-badge--gray">${q.answerOptions.length} отв.</span>
                ${alreadyAdded ? '<span class="lc-badge lc-badge--green">В турнире</span>' : ''}
            </div>
            <div class="lc-item-actions">
                ${pvSelTournamentId && !alreadyAdded && !wrongTopic
                ? `<button class="lc-icon-btn" title="Добавить в турнир"
                            style="background:#dbeafe;color:#2563eb"
                            onclick="pvAddQuestionToTournament(${q.id})">
                            <i class="fa fa-plus"></i></button>`
                : pvSelTournamentId && !alreadyAdded && wrongTopic
                    ? `<span class="lc-badge lc-badge--gray" title="Другая тема">—</span>`
                    : ''}
                <button class="lc-icon-btn lc-edit" title="Редактировать"
                        onclick="pvEditQuestion(${q.id})"><i class="fa fa-pen"></i></button>
                <button class="lc-icon-btn lc-delete" title="Удалить"
                        onclick="pvDeleteQuestion(${q.id})"><i class="fa fa-trash"></i></button>
            </div>
            <button class="lc-select-btn" onclick="pvSelectQuestion(${q.id})">
                Ответы <i class="fa fa-chevron-right"></i>
            </button>
        </div>`;
    }).join('');
}

async function pvAddQuestionToTournament(questionId) {
    if (!pvSelTournamentId) {
        pvShowToast('Сначала выберите турнир слева', 'error');
        return;
    }

    const tournament = PV_TOURNAMENTS.find(x => x.id === pvSelTournamentId);
    const question = PV_QUESTIONS.find(x => x.id === Number(questionId));

    if (tournament && question && tournament.topicId !== question.topicId) {
        pvShowToast(`Вопрос относится к теме «${question.topicName}», а турнир — к теме «${tournament.topicName}». Темы должны совпадать.`, 'error');
        return;
    }

    try {
        const res = await pvPost('/admin/tournaments/questions/add', {
            TournamentId: pvSelTournamentId,
            QuestionId: questionId
        });
        pvShowToast(res.message);
        await pvReload();
    } catch (e) { pvShowToast(e.message, 'error'); }
}

function pvPopulateQuestionForm(q = null) {
    const topicSel = document.getElementById('pv-question-topic');
    const diffSel = document.getElementById('pv-question-difficulty');

    topicSel.innerHTML = PV_TOPICS.map(x =>
        `<option value="${x.id}" ${q && q.topicId === x.id ? 'selected' : ''}>${pvEsc(x.name)}</option>`
    ).join('');

    diffSel.innerHTML = PV_DIFFICULTIES.map(x =>
        `<option value="${x.id}" ${q && q.difficultyTypeId === x.id ? 'selected' : ''}>${pvEsc(x.name)}</option>`
    ).join('');

    document.getElementById('pv-question-id').value = q ? q.id : 0;
    document.getElementById('pv-question-text').value = q ? q.questionText : '';
}

function pvEditQuestion(id) {
    const q = PV_QUESTIONS.find(x => x.id === Number(id));
    if (!q) return;
    pvPopulateQuestionForm(q);
    document.getElementById('pv-modal-question-title').textContent = 'Редактировать вопрос';
    openModal('pv-modal-question');
}

async function pvSaveQuestion() {
    const id = parseInt(document.getElementById('pv-question-id').value);
    const topicId = document.getElementById('pv-question-topic').value;
    const diffId = document.getElementById('pv-question-difficulty').value;
    const text = document.getElementById('pv-question-text').value.trim();

    if (!text) { pvShowToast('Введите текст вопроса', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/questions/create' : '/admin/questions/update';
    const data = isNew
        ? { TopicId: topicId, DifficultyTypeId: diffId, QuestionText: text }
        : { Id: id, TopicId: topicId, DifficultyTypeId: diffId, QuestionText: text };

    try {
        const res = await pvPost(url, data);
        closeModal('pv-modal-question');
        pvShowToast(res.message);
        await pvReload();
    } catch (e) { pvShowToast(e.message, 'error'); }
}

function pvDeleteQuestion(id) {
    const q = PV_QUESTIONS.find(x => x.id === Number(id));
    if (!q) return;
    pvConfirm('Удалить вопрос? Все варианты ответов будут удалены.', async () => {
        try {
            const res = await pvPost(`/admin/questions/delete/${id}`, {});
            pvShowToast(res.message);
            if (pvSelQuestionId === Number(id)) pvLockAnswers();
            await pvReload();
        } catch (e) { pvShowToast(e.message, 'error'); }
    });
}

function pvSelectQuestion(id) {
    pvSelQuestionId = Number(id);
    const q = PV_QUESTIONS.find(x => x.id === pvSelQuestionId);
    if (!q) return;
    document.getElementById('pv-question-label').textContent =
        q.questionText.length > 40 ? q.questionText.substring(0, 40) + '…' : q.questionText;
    document.getElementById('pv-col-answers').classList.remove('lc-col--locked');
    document.getElementById('pv-btn-add-answer').disabled = q.answerOptions.length >= 4;
    pvRenderAnswers(pvSelQuestionId);
}

function pvRenderAnswers(questionId) {
    const q = PV_QUESTIONS.find(x => x.id === questionId);
    const list = document.getElementById('pv-answers-list');

    if (!q) { pvLockAnswers(); return; }

    if (q.answerOptions.length === 0) {
        list.innerHTML = `<div class="lc-empty-state">
            <i class="fa fa-check-circle"></i><p>Вариантов ответа ещё нет</p></div>`;
        return;
    }

    list.innerHTML = q.answerOptions.map(a => `
        <div class="lc-item ${a.isCorrect ? 'pv-answer--correct' : ''}">
            <div class="lc-item-info">
                <span class="lc-item-name">${pvEsc(a.answerText)}</span>
            </div>
            <div class="lc-item-meta">
                ${a.isCorrect
            ? '<span class="lc-badge lc-badge--green"><i class="fa fa-check"></i> Правильный</span>'
            : '<span class="lc-badge lc-badge--gray">Неверный</span>'}
            </div>
            <div class="lc-item-actions">
                <button class="lc-icon-btn lc-edit" title="Редактировать"
                        onclick="pvEditAnswer(${a.id})"><i class="fa fa-pen"></i></button>
                <button class="lc-icon-btn lc-delete" title="Удалить"
                        onclick="pvDeleteAnswer(${a.id})"><i class="fa fa-trash"></i></button>
            </div>
        </div>`).join('');
}

function pvLockAnswers() {
    pvSelQuestionId = null;
    document.getElementById('pv-question-label').textContent = 'Выберите вопрос';
    document.getElementById('pv-btn-add-answer').disabled = true;
    document.getElementById('pv-col-answers').classList.add('lc-col--locked');
    document.getElementById('pv-answers-list').innerHTML = `<div class="lc-empty-state">
        <i class="fa fa-check-circle"></i><p>Выберите вопрос справа</p></div>`;
}

function pvEditAnswer(id) {
    const q = PV_QUESTIONS.find(x => x.id === pvSelQuestionId);
    const a = q?.answerOptions.find(x => x.id === Number(id));
    if (!a) return;
    document.getElementById('pv-answer-id').value = a.id;
    document.getElementById('pv-answer-question-id').value = a.questionId;
    document.getElementById('pv-answer-text').value = a.answerText;
    document.getElementById('pv-answer-correct').checked = a.isCorrect;
    document.getElementById('pv-modal-answer-title').textContent = 'Редактировать ответ';
    openModal('pv-modal-answer');
}

async function pvSaveAnswer() {
    const id = parseInt(document.getElementById('pv-answer-id').value);
    const questionId = document.getElementById('pv-answer-question-id').value;
    const text = document.getElementById('pv-answer-text').value.trim();
    const isCorrect = document.getElementById('pv-answer-correct').checked;

    if (!text) { pvShowToast('Введите текст ответа', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/answers/create' : '/admin/answers/update';
    const data = isNew
        ? { QuestionId: questionId, AnswerText: text, IsCorrect: isCorrect }
        : { Id: id, AnswerText: text, IsCorrect: isCorrect };

    try {
        const res = await pvPost(url, data);
        closeModal('pv-modal-answer');
        pvShowToast(res.message);
        await pvReload();
    } catch (e) { pvShowToast(e.message, 'error'); }
}

function pvDeleteAnswer(id) {
    pvConfirm('Удалить этот вариант ответа?', async () => {
        try {
            const res = await pvPost(`/admin/answers/delete/${id}`, {});
            pvShowToast(res.message);
            await pvReload();
        } catch (e) { pvShowToast(e.message, 'error'); }
    });
}

function pvInit() {
    document.getElementById('pv-btn-add-tournament').addEventListener('click', () => {
        pvPopulateTournamentForm();
        document.getElementById('pv-modal-tournament-title').textContent = 'Новый турнир';
        openModal('pv-modal-tournament');
    });

    document.getElementById('pv-btn-add-question').addEventListener('click', () => {
        pvPopulateQuestionForm();
        document.getElementById('pv-modal-question-title').textContent = 'Новый вопрос';
        openModal('pv-modal-question');
    });

    document.getElementById('pv-btn-add-answer').addEventListener('click', () => {
        if (!pvSelQuestionId) return;
        document.getElementById('pv-answer-id').value = '0';
        document.getElementById('pv-answer-question-id').value = pvSelQuestionId;
        document.getElementById('pv-answer-text').value = '';
        document.getElementById('pv-answer-correct').checked = false;
        document.getElementById('pv-modal-answer-title').textContent = 'Новый вариант ответа';
        openModal('pv-modal-answer');
    });

    document.querySelectorAll('.lc-modal-overlay').forEach(o =>
        o.addEventListener('click', e => { if (e.target === o) o.classList.remove('active'); })
    );

    pvRenderQuestionBank();
}