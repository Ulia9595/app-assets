let tkSelTopicId = null;
let tkSelLevelId = null;
let tkSelTaskId = null;
let tkActiveTab = 'info';

function tkEsc(s) {
    return String(s ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function tkEscJs(s) { return String(s ?? '').replace(/\\/g, '\\\\').replace(/'/g, "\\'"); }

function tkShowToast(msg, type = 'success') {
    const t = document.getElementById('tk-toast');
    t.textContent = msg;
    t.className = 'lc-toast lc-toast--' + type + ' lc-toast--show';
    clearTimeout(t._timer);
    t._timer = setTimeout(() => t.classList.remove('lc-toast--show'), 3200);
}

function tkConfirm(text, cb) {
    document.getElementById('tk-confirm-text').textContent = text;
    const btn = document.getElementById('tk-btn-confirm');
    btn.onclick = () => { closeModal('tk-modal-confirm'); cb(); };
    openModal('tk-modal-confirm');
}

async function tkPostForm(url, data) {
    const fd = new FormData();
    fd.append('__RequestVerificationToken', TK_TOKEN);
    Object.entries(data).forEach(([k, v]) => { if (v !== undefined && v !== null) fd.append(k, v); });
    const res = await fetch(url, { method: 'POST', body: fd });
    const json = await res.json();
    if (!res.ok) throw new Error(json.error || 'Ошибка запроса');
    return json;
}

function tkFindLevel(levelId) {
    for (const t of TK_TOPICS)
        for (const l of t.levels)
            if (l.id === levelId) return { topic: t, level: l };
    return null;
}

function tkFindTask(taskId) {
    for (const t of TK_TOPICS)
        for (const l of t.levels)
            for (const task of l.tasks)
                if (task.id === taskId) return task;
    return null;
}

function tkDiffBadge(name) {
    if (name === 'Лёгкий') return 'lc-badge--green';
    if (name === 'Средний') return 'lc-badge--blue';
    if (name === 'Сложный') return 'lc-badge--red';
    return '';
}

function tkGetComposition(tasks) {
    return {
        total: tasks.length,
        easy: tasks.filter(t => t.difficultyTypeName === 'Лёгкий').length,
        medium: tasks.filter(t => t.difficultyTypeName === 'Средний').length,
        hard: tasks.filter(t => t.difficultyTypeName === 'Сложный').length
    };
}

function tkGetCompositionHints(c) {
    const hints = [];
    if (c.easy < 1) hints.push('нужно минимум 1 лёгкое');
    if (c.easy < 3 && c.total < 5) hints.push(`можно добавить ещё ${Math.min(3 - c.easy, 5 - c.total)} лёгких`);
    if (c.medium < 1) hints.push('нужно 1 среднее');
    if (c.hard < 1) hints.push('нужно 1 сложное');
    return hints;
}

function selectLevel(levelId, levelName, topicId) {
    tkSelLevelId = levelId;
    tkSelTopicId = topicId;
    tkSelTaskId = null;

    document.querySelectorAll('.tk-level-item').forEach(el => el.classList.remove('tk-active'));
    document.querySelector(`.tk-level-item[data-level-id="${levelId}"]`)?.classList.add('tk-active');

    document.getElementById('tk-level-label').textContent = levelName;
    document.getElementById('tk-btn-add-task').disabled = false;
    document.getElementById('tk-col-tasks').classList.remove('lc-col--locked');
    tkRenderTasks(levelId);
    tkLockDetails();
}

function tkRenderTasks(levelId) {
    const found = tkFindLevel(levelId);
    const list = document.getElementById('tk-tasks-list');
    const btn = document.getElementById('tk-btn-add-task');

    if (!found) {
        list.innerHTML = `<div class="lc-empty-state"><i class="fa fa-code"></i><p>В этом уровне ещё нет заданий</p></div>`;
        btn.disabled = false;
        return;
    }

    const tasks = found.level.tasks;
    const c = tkGetComposition(tasks);
    const full = c.total >= 5;
    const hints = tkGetCompositionHints(c);

    btn.disabled = full;
    btn.title = full ? 'Уровень содержит максимальное количество заданий (5)' : '';

    const compositionBar = `
        <div class="tk-composition">
            <div class="tk-composition-counts">
                <span class="tk-comp-item ${c.easy >= 1 ? 'tk-comp--ok' : 'tk-comp--warn'}">Лёгких: ${c.easy}/3</span>
                <span class="tk-comp-item ${c.medium === 1 ? 'tk-comp--ok' : 'tk-comp--warn'}">Среднее: ${c.medium}/1</span>
                <span class="tk-comp-item ${c.hard === 1 ? 'tk-comp--ok' : 'tk-comp--warn'}">Сложное: ${c.hard}/1</span>
                <span class="tk-comp-item ${c.total >= 3 && c.total <= 5 ? 'tk-comp--ok' : 'tk-comp--warn'}">Всего: ${c.total}/5</span>
            </div>
            ${hints.length > 0 ? `<div class="tk-composition-hint">${hints.join(', ')}</div>` : ''}
        </div>`;

    if (tasks.length === 0) {
        list.innerHTML = compositionBar + `<div class="lc-empty-state"><i class="fa fa-code"></i><p>В этом уровне ещё нет заданий</p></div>`;
        return;
    }

    list.innerHTML = compositionBar + tasks.map(task => `
        <div class="lc-item tk-task-item" data-task-id="${task.id}">
            <div class="lc-item-info">
                <span class="lc-item-order">#${task.displayOrder}</span>
                <span class="lc-item-name">${tkEsc(task.name)}</span>
                <span class="lc-item-desc">${tkEsc(task.difficultyTypeName)} · ${tkEsc(task.checkTypeName)}</span>
            </div>
            <div class="lc-item-meta">
                <span class="lc-badge ${tkDiffBadge(task.difficultyTypeName)}">${tkEsc(task.difficultyTypeName)}</span>
                <span class="lc-badge lc-badge--gray">${task.testCases.length} тк</span>
                <span class="lc-badge lc-badge--gray">${task.hints.length} подск</span>
            </div>
            <div class="lc-item-actions">
                <button class="lc-icon-btn lc-edit" title="Редактировать"
                        onclick="tkEditTask(${task.id})"><i class="fa fa-pen"></i></button>
                <button class="lc-icon-btn lc-delete" title="Удалить"
                        onclick="tkDeleteTask(${task.id}, '${tkEscJs(task.name)}')"><i class="fa fa-trash"></i></button>
            </div>
            <button class="lc-select-btn" onclick="selectTask(${task.id}, '${tkEscJs(task.name)}')">
                Детали <i class="fa fa-chevron-right"></i>
            </button>
        </div>`).join('');
}

function selectTask(taskId, taskName) {
    tkSelTaskId = taskId;
    const task = tkFindTask(taskId);
    document.getElementById('tk-task-label').textContent = taskName;
    document.getElementById('tk-col-details').classList.remove('lc-col--locked');
    document.getElementById('tk-tabs').style.display = '';

    const isCustom = task?.checkTypeName === 'Кастомная проверка';
    document.getElementById('tk-tab-btn-testcases').style.display = isCustom ? 'none' : '';
    document.getElementById('tk-tab-btn-algorithm').style.display = isCustom ? '' : 'none';

    if ((tkActiveTab === 'testcases' && isCustom) ||
        (tkActiveTab === 'algorithm' && !isCustom)) {
        tkActiveTab = 'info';
        document.querySelectorAll('.tk-tab').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tk-tab-pane').forEach(p => p.style.display = 'none');
        document.getElementById('tk-tab-info').style.display = '';
        document.querySelector('.tk-tab[data-tab="info"]').classList.add('active');
    }

    tkRenderTaskDetails(taskId);
}

function tkLockDetails() {
    tkSelTaskId = null;
    tkActiveTab = 'info';
    document.getElementById('tk-task-label').textContent = 'Выберите задание';
    document.getElementById('tk-col-details').classList.add('lc-col--locked');
    document.getElementById('tk-tabs').style.display = 'none';
    document.getElementById('tk-tab-btn-testcases').style.display = '';
    document.getElementById('tk-tab-btn-algorithm').style.display = '';
    ['info', 'testcases', 'hints', 'algorithm'].forEach(tab => {
        const pane = document.getElementById('tk-tab-' + tab);
        pane.innerHTML = '<div class="lc-empty-state"><i class="fa fa-code"></i><p>Выберите задание</p></div>';
        pane.style.display = tab === 'info' ? '' : 'none';
    });
    document.querySelectorAll('.tk-tab').forEach((b, i) => b.classList.toggle('active', i === 0));
}

function tkRenderTaskDetails(taskId) {
    const task = tkFindTask(taskId);
    if (!task) return;
    if (tkActiveTab === 'info') tkRenderInfo(task);
    if (tkActiveTab === 'testcases') tkRenderTestCases(task);
    if (tkActiveTab === 'hints') tkRenderHints(task);
    if (tkActiveTab === 'algorithm') tkRenderAlgorithm(task);
}

function tkRenderInfo(task) {
    document.getElementById('tk-tab-info').innerHTML = `
        <div class="tk-info-card">
            <div class="tk-info-row">
                <span class="tk-info-label">Название</span>
                <span class="tk-info-value">${tkEsc(task.name)}</span>
            </div>
            <div class="tk-info-row">
                <span class="tk-info-label">Сложность</span>
                <span class="tk-info-value">
                    <span class="lc-badge ${tkDiffBadge(task.difficultyTypeName)}">${tkEsc(task.difficultyTypeName)}</span>
                </span>
            </div>
            <div class="tk-info-row">
                <span class="tk-info-label">Тип проверки</span>
                <span class="tk-info-value">${tkEsc(task.checkTypeName)}</span>
            </div>
            <div class="tk-info-row">
                <span class="tk-info-label">Порядок</span>
                <span class="tk-info-value">${task.displayOrder}</span>
            </div>
            <div class="tk-info-row tk-info-row--full">
                <span class="tk-info-label">Условие</span>
                <pre class="tk-condition-text">${tkEsc(task.condition)}</pre>
            </div>
        </div>`;
}

function tkRenderTestCases(task) {
    const pane = document.getElementById('tk-tab-testcases');
    let html = `<div class="tk-tab-toolbar">
        <button class="lc-btn lc-btn--primary lc-btn--sm" onclick="tkAddTestCase(${task.id})">
            <i class="fa fa-plus"></i> Добавить тест-кейс
        </button>
    </div>`;

    if (task.testCases.length === 0) {
        html += `<div class="lc-empty-state"><i class="fa fa-vial"></i><p>Тест-кейсов ещё нет</p></div>`;
    } else {
        html += task.testCases.map((tc, i) => `
            <div class="tk-testcase-card">
                <div class="tk-tc-header">
                    <span class="tk-tc-num">Тест ${i + 1}</span>
                    ${tc.isHidden ? '<span class="lc-badge lc-badge--gray">Скрытый</span>' : '<span class="lc-badge lc-badge--green">Открытый</span>'}
                    <div class="lc-item-actions" style="margin-left:auto">
                        <button class="lc-icon-btn lc-edit" onclick="tkEditTestCase(${tc.id},${task.id})"><i class="fa fa-pen"></i></button>
                        <button class="lc-icon-btn lc-delete" onclick="tkDeleteTestCase(${tc.id})"><i class="fa fa-trash"></i></button>
                    </div>
                </div>
                <div class="tk-tc-row">
                    <span class="tk-tc-label">Вход</span>
                    <pre class="tk-tc-value">${tc.inputData ? tkEsc(tc.inputData) : '<span style="color:#94a3b8">—</span>'}</pre>
                </div>
                <div class="tk-tc-row">
                    <span class="tk-tc-label">Вывод</span>
                    <pre class="tk-tc-value">${tkEsc(tc.expectedOutput)}</pre>
                </div>
            </div>`).join('');
    }
    pane.innerHTML = html;
}

function tkRenderHints(task) {
    const pane = document.getElementById('tk-tab-hints');
    let html = `<div class="tk-tab-toolbar">
        <button class="lc-btn lc-btn--primary lc-btn--sm" onclick="tkAddHint(${task.id})">
            <i class="fa fa-plus"></i> Добавить подсказку
        </button>
    </div>`;

    if (task.hints.length === 0) {
        html += `<div class="lc-empty-state"><i class="fa fa-lightbulb"></i><p>Подсказок ещё нет</p></div>`;
    } else {
        html += task.hints.map((h, i) => `
            <div class="tk-hint-card">
                <div class="tk-tc-header">
                    <span class="tk-tc-num">Подсказка ${i + 1}</span>
                    <span class="lc-badge lc-badge--gray">Порядок: ${h.displayOrder}</span>
                    <div class="lc-item-actions" style="margin-left:auto">
                        <button class="lc-icon-btn lc-edit" onclick="tkEditHint(${h.id},${task.id})"><i class="fa fa-pen"></i></button>
                        <button class="lc-icon-btn lc-delete" onclick="tkDeleteHint(${h.id})"><i class="fa fa-trash"></i></button>
                    </div>
                </div>
                <p class="tk-hint-text">${tkEsc(h.hintText)}</p>
            </div>`).join('');
    }
    pane.innerHTML = html;
}

function tkRenderAlgorithm(task) {
    const pane = document.getElementById('tk-tab-algorithm');
    const algo = task.customCheckAlgorithm;

    if (!algo) {
        pane.innerHTML = `
            <div class="tk-tab-toolbar">
                <button class="lc-btn lc-btn--primary lc-btn--sm" onclick="tkAddAlgorithm(${task.id})">
                    <i class="fa fa-plus"></i> Добавить алгоритм
                </button>
            </div>
            <div class="lc-empty-state">
                <i class="fa fa-cog"></i>
                <p>Алгоритм кастомной проверки ещё не добавлен</p>
            </div>`;
    } else {
        pane.innerHTML = `
            <div class="tk-testcase-card">
                <div class="tk-tc-header">
                    <span class="tk-tc-num">Алгоритм проверки</span>
                    <div class="lc-item-actions" style="margin-left:auto">
                        <button class="lc-icon-btn lc-edit" onclick="tkEditAlgorithm(${algo.id}, ${task.id})"><i class="fa fa-pen"></i></button>
                        <button class="lc-icon-btn lc-delete" onclick="tkDeleteAlgorithm(${algo.id})"><i class="fa fa-trash"></i></button>
                    </div>
                </div>
                <pre class="tk-code-preview">${tkEsc(algo.algorithmCode)}</pre>
            </div>`;
    }
}

function tkPopulateLevelSelect(selectedLevelId) {
    const sel = document.getElementById('tk-task-level-select');
    sel.innerHTML = TK_TOPICS.flatMap(t =>
        t.levels.map(l =>
            `<option value="${l.id}" ${l.id === selectedLevelId ? 'selected' : ''}>${tkEsc(t.name)} — ${tkEsc(l.name)}</option>`
        )
    ).join('');
}

function tkPopulateDifficultySelect(selectedId) {
    const sel = document.getElementById('tk-task-difficulty');
    sel.innerHTML = DIFFICULTY.map(d =>
        `<option value="${d.id}" ${d.id === selectedId ? 'selected' : ''}>${tkEsc(d.name)}</option>`
    ).join('');
}

function tkPopulateCheckTypeSelect(selectedId) {
    const sel = document.getElementById('tk-task-checktype');
    sel.innerHTML = CHECK_TYPES.map(c =>
        `<option value="${c.id}" ${c.id === selectedId ? 'selected' : ''}>${tkEsc(c.name)}</option>`
    ).join('');
}

function tkEditTask(taskId) {
    const task = tkFindTask(taskId);
    if (!task) return;
    document.getElementById('tk-task-id').value = task.id;
    document.getElementById('tk-task-name').value = task.name;
    document.getElementById('tk-task-condition').value = task.condition;
    document.getElementById('tk-task-order').value = task.displayOrder;
    tkPopulateLevelSelect(task.levelId);
    tkPopulateDifficultySelect(task.difficultyTypeId);
    tkPopulateCheckTypeSelect(task.checkTypeId);
    document.getElementById('tk-modal-task-title').textContent = 'Редактировать задание';
    openModal('tk-modal-task');
}

async function tkSaveTask() {
    const id = parseInt(document.getElementById('tk-task-id').value);
    const levelId = parseInt(document.getElementById('tk-task-level-select').value);
    const name = document.getElementById('tk-task-name').value.trim();
    const condition = document.getElementById('tk-task-condition').value.trim();
    const order = parseInt(document.getElementById('tk-task-order').value);
    const diffId = parseInt(document.getElementById('tk-task-difficulty').value);
    const checkId = parseInt(document.getElementById('tk-task-checktype').value);

    if (!name) { tkShowToast('Введите название задания', 'error'); return; }
    if (!condition) { tkShowToast('Введите условие задания', 'error'); return; }
    if (isNaN(order) || order < 0) { tkShowToast('Укажите корректный порядковый номер', 'error'); return; }

    const level = TK_TOPICS.flatMap(t => t.levels).find(l => l.id === levelId);
    if (level) {
        const conflict = level.tasks.find(t => t.displayOrder === order && t.id !== id);
        if (conflict) {
            tkShowToast(`Порядковый номер ${order} уже занят заданием «${conflict.name}» — остальные будут сдвинуты вниз`, 'info');
        }
    }

    const isNew = id === 0;
    const url = isNew ? '/admin/tasks/create' : '/admin/tasks/update';
    const data = isNew
        ? { LevelId: levelId, Name: name, Condition: condition, DifficultyTypeId: diffId, CheckTypeId: checkId, DisplayOrder: order }
        : { Id: id, LevelId: levelId, Name: name, Condition: condition, DifficultyTypeId: diffId, CheckTypeId: checkId, DisplayOrder: order };

    try {
        const res = await tkPostForm(url, data);
        closeModal('tk-modal-task');
        tkShowToast(res.message);
        await tkReload();
    } catch (e) { tkShowToast(e.message, 'error'); }
}

function tkDeleteTask(taskId, name) {
    const found = tkFindLevel(tkSelLevelId);
    const tasks = found ? found.level.tasks : [];
    const remainingCount = tasks.length - 1;

    const warningText = remainingCount < 3
        ? `После удаления в уровне останется ${remainingCount} задан. (рекомендуется минимум 3).`
        : '';

    tkConfirm(
        `Удалить задание «${name}»? Все тест-кейсы, подсказки и алгоритм также будут удалены.${warningText}`,
        async () => {
            try {
                const res = await tkPostForm(`/admin/tasks/delete/${taskId}`, {});
                tkShowToast(res.message);
                if (tkSelTaskId === taskId) tkLockDetails();
                await tkReload();
            } catch (e) { tkShowToast(e.message, 'error'); }
        }
    );
}

function tkAddTestCase(taskId) {
    document.getElementById('tk-tc-id').value = '0';
    document.getElementById('tk-tc-task-id').value = taskId;
    document.getElementById('tk-tc-input').value = '';
    document.getElementById('tk-tc-output').value = '';
    document.getElementById('tk-tc-hidden').checked = false;
    document.getElementById('tk-modal-tc-title').textContent = 'Новый тест-кейс';
    openModal('tk-modal-testcase');
}

function tkEditTestCase(tcId, taskId) {
    const task = tkFindTask(taskId);
    const tc = task?.testCases.find(t => t.id === tcId);
    if (!tc) return;
    document.getElementById('tk-tc-id').value = tc.id;
    document.getElementById('tk-tc-task-id').value = tc.taskId;
    document.getElementById('tk-tc-input').value = tc.inputData ?? '';
    document.getElementById('tk-tc-output').value = tc.expectedOutput;
    document.getElementById('tk-tc-hidden').checked = tc.isHidden;
    document.getElementById('tk-modal-tc-title').textContent = 'Редактировать тест-кейс';
    openModal('tk-modal-testcase');
}

async function tkSaveTestCase() {
    const id = parseInt(document.getElementById('tk-tc-id').value);
    const taskId = document.getElementById('tk-tc-task-id').value;
    const input = document.getElementById('tk-tc-input').value;
    const output = document.getElementById('tk-tc-output').value.trim();
    const isHidden = document.getElementById('tk-tc-hidden').checked;

    if (!output) { tkShowToast('Введите ожидаемый вывод', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/testcases/create' : '/admin/testcases/update';
    const data = isNew
        ? { TaskId: taskId, InputData: input, ExpectedOutput: output, IsHidden: isHidden }
        : { Id: id, InputData: input, ExpectedOutput: output, IsHidden: isHidden };

    try {
        const res = await tkPostForm(url, data);
        closeModal('tk-modal-testcase');
        tkShowToast(res.message);
        await tkReload();
    } catch (e) { tkShowToast(e.message, 'error'); }
}

function tkDeleteTestCase(tcId) {
    tkConfirm('Удалить этот тест-кейс?', async () => {
        try {
            const res = await tkPostForm(`/admin/testcases/delete/${tcId}`, {});
            tkShowToast(res.message);
            await tkReload();
        } catch (e) { tkShowToast(e.message, 'error'); }
    });
}

function tkAddHint(taskId) {
    const task = tkFindTask(taskId);
    document.getElementById('tk-hint-id').value = '0';
    document.getElementById('tk-hint-task-id').value = taskId;
    document.getElementById('tk-hint-text').value = '';
    document.getElementById('tk-hint-order').value = task ? task.hints.length : 0;
    document.getElementById('tk-modal-hint-title').textContent = 'Новая подсказка';
    openModal('tk-modal-hint');
}

function tkEditHint(hintId, taskId) {
    const task = tkFindTask(taskId);
    const hint = task?.hints.find(h => h.id === hintId);
    if (!hint) return;
    document.getElementById('tk-hint-id').value = hint.id;
    document.getElementById('tk-hint-task-id').value = hint.taskId;
    document.getElementById('tk-hint-text').value = hint.hintText;
    document.getElementById('tk-hint-order').value = hint.displayOrder;
    document.getElementById('tk-modal-hint-title').textContent = 'Редактировать подсказку';
    openModal('tk-modal-hint');
}

async function tkSaveHint() {
    const id = parseInt(document.getElementById('tk-hint-id').value);
    const taskId = parseInt(document.getElementById('tk-hint-task-id').value);
    const text = document.getElementById('tk-hint-text').value.trim();
    const order = parseInt(document.getElementById('tk-hint-order').value);

    if (!text) { tkShowToast('Введите текст подсказки', 'error'); return; }
    if (isNaN(order) || order < 0) { tkShowToast('Укажите корректный порядковый номер', 'error'); return; }

    const task = TK_TOPICS.flatMap(t => t.levels).flatMap(l => l.tasks).find(t => t.id === taskId);
    if (task?.hints) {
        const conflict = task.hints.find(h => h.displayOrder === order && h.id !== id);
        if (conflict) {
            tkShowToast(`Порядковый номер ${order} занят — остальные подсказки будут сдвинуты вниз`, 'info');
        }
    }

    const isNew = id === 0;
    const url = isNew ? '/admin/hints/create' : '/admin/hints/update';
    const data = isNew
        ? { TaskId: taskId, HintText: text, DisplayOrder: order }
        : { Id: id, HintText: text, DisplayOrder: order };

    try {
        const res = await tkPostForm(url, data);
        closeModal('tk-modal-hint');
        tkShowToast(res.message);
        await tkReload();
    } catch (e) { tkShowToast(e.message, 'error'); }
}

function tkDeleteHint(hintId) {
    tkConfirm('Удалить эту подсказку?', async () => {
        try {
            const res = await tkPostForm(`/admin/hints/delete/${hintId}`, {});
            tkShowToast(res.message);
            await tkReload();
        } catch (e) { tkShowToast(e.message, 'error'); }
    });
}

function tkAddAlgorithm(taskId) {
    document.getElementById('tk-algo-id').value = '0';
    document.getElementById('tk-algo-task-id').value = taskId;
    document.getElementById('tk-algo-code').value = '';
    document.getElementById('tk-modal-algo-title').textContent = 'Добавить алгоритм проверки';
    openModal('tk-modal-algorithm');
}

function tkEditAlgorithm(algoId, taskId) {
    const task = tkFindTask(taskId);
    const algo = task?.customCheckAlgorithm;
    if (!algo) return;
    document.getElementById('tk-algo-id').value = algo.id;
    document.getElementById('tk-algo-task-id').value = algo.taskId;
    document.getElementById('tk-algo-code').value = algo.algorithmCode;
    document.getElementById('tk-modal-algo-title').textContent = 'Редактировать алгоритм проверки';
    openModal('tk-modal-algorithm');
}

async function tkSaveAlgorithm() {
    const id = parseInt(document.getElementById('tk-algo-id').value);
    const taskId = document.getElementById('tk-algo-task-id').value;
    const code = document.getElementById('tk-algo-code').value.trim();

    if (!code) { tkShowToast('Введите код алгоритма', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/algorithms/create' : '/admin/algorithms/update';
    const data = isNew
        ? { TaskId: taskId, AlgorithmCode: code }
        : { Id: id, AlgorithmCode: code };

    try {
        const res = await tkPostForm(url, data);
        closeModal('tk-modal-algorithm');
        tkShowToast(res.message);
        await tkReload();
    } catch (e) { tkShowToast(e.message, 'error'); }
}

function tkDeleteAlgorithm(algoId) {
    tkConfirm('Удалить алгоритм проверки?', async () => {
        try {
            const res = await tkPostForm(`/admin/algorithms/delete/${algoId}`, {});
            tkShowToast(res.message);
            await tkReload();
        } catch (e) { tkShowToast(e.message, 'error'); }
    });
}

async function tkReload() {
    try {
        const res = await fetch('/admin/tasks', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        const html = await res.text();
        const doc = new DOMParser().parseFromString(html, 'text/html');

        let fresh = null;
        doc.querySelectorAll('script').forEach(s => {
            const m = s.textContent.match(/const TK_TOPICS\s*=\s*(\[[\s\S]*?\]);/);
            if (m) { try { fresh = JSON.parse(m[1]); } catch { } }
        });

        if (fresh) {
            TK_TOPICS.length = 0;
            fresh.forEach(t => TK_TOPICS.push(t));
        }

        const newList = doc.getElementById('tk-levels-list');
        if (newList) document.getElementById('tk-levels-list').innerHTML = newList.innerHTML;

        if (tkSelLevelId) {
            const found = tkFindLevel(tkSelLevelId);
            if (found) tkRenderTasks(tkSelLevelId);
        }

        if (tkSelTaskId) {
            const task = tkFindTask(tkSelTaskId);
            if (task) tkRenderTaskDetails(tkSelTaskId);
            else tkLockDetails();
        }
    } catch {
        tkShowToast('Ошибка обновления данных', 'error');
    }
}

function tkInit() {
    document.getElementById('tk-btn-add-task').addEventListener('click', () => {
        if (!tkSelLevelId) return;
        const found = tkFindLevel(tkSelLevelId);
        const tasks = found ? found.level.tasks : [];
        const c = tkGetComposition(tasks);

        const sel = document.getElementById('tk-task-difficulty');
        sel.innerHTML = DIFFICULTY
            .filter(d => {
                if (d.name === 'Лёгкий' && c.easy >= 3) return false;
                if (d.name === 'Средний' && c.medium >= 1) return false;
                if (d.name === 'Сложный' && c.hard >= 1) return false;
                return true;
            })
            .map(d => `<option value="${d.id}">${tkEsc(d.name)}</option>`)
            .join('');

        document.getElementById('tk-task-id').value = '0';
        document.getElementById('tk-task-name').value = '';
        document.getElementById('tk-task-condition').value = '';
        document.getElementById('tk-task-order').value = tasks.length;
        tkPopulateLevelSelect(tkSelLevelId);
        tkPopulateCheckTypeSelect(CHECK_TYPES[0]?.id);
        document.getElementById('tk-modal-task-title').textContent = 'Новое задание';
        openModal('tk-modal-task');
    });

    document.querySelectorAll('.tk-tab').forEach(btn => {
        btn.addEventListener('click', () => {
            tkActiveTab = btn.dataset.tab;
            document.querySelectorAll('.tk-tab').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            document.querySelectorAll('.tk-tab-pane').forEach(p => p.style.display = 'none');
            document.getElementById('tk-tab-' + tkActiveTab).style.display = '';
            if (tkSelTaskId) tkRenderTaskDetails(tkSelTaskId);
        });
    });

    document.querySelectorAll('.lc-modal-overlay').forEach(o =>
        o.addEventListener('click', e => { if (e.target === o) o.classList.remove('active'); })
    );
}
