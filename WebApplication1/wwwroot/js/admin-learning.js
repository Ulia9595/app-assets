function lcShowToast(msg, type = 'success') {
    const t = document.getElementById('lc-toast');
    if (!t) { console.warn('lcShowToast: #lc-toast not found', msg); return; }
    t.textContent = msg;
    t.className = 'lc-toast lc-toast--' + type + ' lc-toast--show';
    clearTimeout(t._timer);
    t._timer = setTimeout(() => t.classList.remove('lc-toast--show'), 3200);
}

function openModal(id) { document.getElementById(id).classList.add('active'); }
function closeModal(id) { document.getElementById(id).classList.remove('active'); }

function lcConfirmAction(text, cb) {
    document.getElementById('confirm-text').textContent = text;
    const btn = document.getElementById('btn-confirm-action');
    btn.onclick = () => { closeModal('modal-confirm'); cb(); };
    openModal('modal-confirm');
}

async function lcPostForm(url, data) {
    const fd = new FormData();
    fd.append('__RequestVerificationToken', LC_TOKEN);
    Object.entries(data).forEach(([k, v]) => fd.append(k, v ?? ''));
    const res = await fetch(url, { method: 'POST', body: fd });
    const json = await res.json();
    if (!res.ok) throw new Error(json.error || 'Ошибка запроса');
    return json;
}

function lcEsc(s) {
    return String(s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}
function lcEscJs(s) {
    return String(s).replace(/\\/g, '\\\\').replace(/'/g, "\\'");
}

let selectedTopicId = null;
let selectedLevelId = null;

function editTopic(id) {
    const t = ALL_TOPICS.find(x => x.id === id);
    if (!t) return;
    document.getElementById('topic-id').value = t.id;
    document.getElementById('topic-name').value = t.name;
    document.getElementById('topic-description').value = t.description || '';
    document.getElementById('topic-order').value = t.displayOrder;
    document.getElementById('modal-topic-title').textContent = 'Редактировать тему';
    openModal('modal-topic');
}

function viewTopic(id) {
    const t = ALL_TOPICS.find(x => x.id === id);
    if (!t) return;
    document.getElementById('view-topic-name').textContent = t.name;
    document.getElementById('view-topic-desc').textContent = t.description || '— не указано —';
    document.getElementById('view-topic-order').textContent = t.displayOrder;
    document.getElementById('view-topic-levels').textContent = t.levels.length + ' шт.';
    openModal('modal-view-topic');
}

async function saveTopic() {
    const id = parseInt(document.getElementById('topic-id').value);
    const name = document.getElementById('topic-name').value.trim();
    const desc = document.getElementById('topic-description').value.trim();
    const order = parseInt(document.getElementById('topic-order').value);

    if (!name) { lcShowToast('Введите название темы', 'error'); return; }
    if (isNaN(order) || order < 0) { lcShowToast('Укажите корректный порядковый номер', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/topics/create' : '/admin/topics/update';
    const data = isNew
        ? { Name: name, Description: desc, DisplayOrder: order }
        : { Id: id, Name: name, Description: desc, DisplayOrder: order };

    try {
        const res = await lcPostForm(url, data);
        closeModal('modal-topic');
        lcShowToast(res.message);
        await lcReloadData();
    } catch (e) { lcShowToast(e.message, 'error'); }
}

function deleteTopic(id, name, levelsCount) {
    if (levelsCount > 0) {
        lcShowToast('Нельзя удалить тему с уровнями — сначала удалите все уровни', 'error');
        return;
    }
    lcConfirmAction(`Удалить тему «${name}»? Это действие необратимо.`, async () => {
        try {
            const res = await lcPostForm(`/admin/topics/delete/${id}`, {});
            lcShowToast(res.message);
            if (selectedTopicId === id) {
                selectedTopicId = null;
                lcLockLevels();
                lcLockTheory();
            }
            await lcReloadData();
        } catch (e) { lcShowToast(e.message, 'error'); }
    });
}

function selectTopic(id, name) {
    selectedTopicId = id;
    selectedLevelId = null;
    document.getElementById('levels-topic-label').textContent = name;
    document.getElementById('btn-add-level').disabled = false;
    document.getElementById('col-levels').classList.remove('lc-col--locked');
    lcRenderLevels(id);
    lcLockTheory();
}

function lcRenderLevels(topicId) {
    const topic = ALL_TOPICS.find(t => t.id === topicId);
    const list = document.getElementById('levels-list');

    if (!topic || topic.levels.length === 0) {
        list.innerHTML = `<div class="lc-empty-state">
            <i class="fa fa-layer-group"></i>
            <p>В этой теме пока нет уровней</p></div>`;
        return;
    }

    list.innerHTML = topic.levels.map(l => `
        <div class="lc-item" data-id="${l.id}">
            <div class="lc-item-info">
                <span class="lc-item-order">${l.levelNumber}</span>
                <span class="lc-item-name">${lcEsc(l.name)}</span>
            </div>
            <div class="lc-item-meta">
                <span class="lc-badge ${l.theory ? 'lc-badge--green' : 'lc-badge--gray'}">
                    ${l.theory ? 'Теория ✓' : 'Нет теории'}
                </span>
            </div>
            <div class="lc-item-actions">
                <button class="lc-icon-btn lc-view" title="Просмотр"
                        onclick="event.stopPropagation(); viewLevel(${l.id}, ${topicId})"><i class="fa fa-eye"></i></button>
                <button class="lc-icon-btn lc-edit" title="Редактировать"
                        onclick="event.stopPropagation(); editLevel(${l.id})"><i class="fa fa-pen"></i></button>
                <button class="lc-icon-btn lc-delete" title="Удалить"
                        onclick="event.stopPropagation(); deleteLevel(${l.id}, '${lcEscJs(l.name)}')"><i class="fa fa-trash"></i></button>
            </div>
            <button class="lc-select-btn" onclick="event.stopPropagation(); lcSelectLevel(${l.id}, '${lcEscJs(l.name)}', ${topicId})">
                Теория <i class="fa fa-chevron-right"></i>
            </button>
        </div>`).join('');
}

function lcLockLevels() {
    document.getElementById('levels-topic-label').textContent = 'Выберите тему';
    document.getElementById('btn-add-level').disabled = true;
    document.getElementById('col-levels').classList.add('lc-col--locked');
    document.getElementById('levels-list').innerHTML = `<div class="lc-empty-state">
        <i class="fa fa-layer-group"></i>
        <p>Выберите тему слева, чтобы увидеть её уровни</p></div>`;
}

function editLevel(id) {
    const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
    const level = topic?.levels.find(l => l.id === id);
    if (!level) return;
    document.getElementById('level-id').value = level.id;
    document.getElementById('level-name').value = level.name;
    document.getElementById('level-number').value = level.levelNumber;
    lcPopulateTopicSelect(level.topicId);
    document.getElementById('modal-level-title').textContent = 'Редактировать уровень';
    openModal('modal-level');
}

function viewLevel(id, topicId) {
    const topic = ALL_TOPICS.find(t => t.id === topicId);
    const l = topic?.levels.find(l => l.id === id);
    if (!l) return;
    document.getElementById('view-level-name').textContent = l.name;
    document.getElementById('view-level-number').textContent = l.levelNumber;
    document.getElementById('view-level-topic').textContent = topic?.name ?? '—';
    document.getElementById('view-level-theory').textContent = l.theory
        ? l.theory.title
        : '— теория не добавлена —';
    openModal('modal-view-level');
}

function lcPopulateTopicSelect(selectedId) {
    const sel = document.getElementById('level-topic-select');
    sel.innerHTML = ALL_TOPICS.map(t =>
        `<option value="${t.id}" ${t.id === selectedId ? 'selected' : ''}>${lcEsc(t.name)}</option>`
    ).join('');
}

async function saveLevel() {
    const id = parseInt(document.getElementById('level-id').value);
    const topicId = parseInt(document.getElementById('level-topic-select').value);
    const name = document.getElementById('level-name').value.trim();
    const num = parseInt(document.getElementById('level-number').value);

    if (!name) { lcShowToast('Введите название уровня', 'error'); return; }
    if (isNaN(num) || num < 1) { lcShowToast('Укажите корректный номер уровня', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/levels/create' : '/admin/levels/update';
    const data = isNew
        ? { TopicId: topicId, Name: name, LevelNumber: num }
        : { Id: id, TopicId: topicId, Name: name, LevelNumber: num };

    try {
        const res = await lcPostForm(url, data);
        closeModal('modal-level');
        lcShowToast(res.message);
        await lcReloadData();
    } catch (e) { lcShowToast(e.message, 'error'); }
}

function deleteLevel(id, name) {
    lcConfirmAction(
        `Удалить уровень «${name}»? Привязанная теория также будет удалена.`,
        async () => {
            try {
                const res = await lcPostForm(`/admin/levels/delete/${id}`, {});
                lcShowToast(res.message);
                if (selectedLevelId === id) lcLockTheory();
                await lcReloadData();
            } catch (e) { lcShowToast(e.message, 'error'); }
        }
    );
}

function lcSelectLevel(id, name, topicId) {
    console.log('lcSelectLevel called', id, name, topicId);
    selectedLevelId = id;
    selectedTopicId = topicId;
    document.getElementById('theory-level-label').textContent = name;
    document.getElementById('col-theory').classList.remove('lc-col--locked');
    lcRenderTheory(id, topicId);
    const topic = ALL_TOPICS.find(t => t.id === topicId);
    const level = topic?.levels.find(l => l.id === id);
    console.log('found topic:', topic);
    console.log('found level:', level);
    console.log('theory:', level?.theory);
    document.getElementById('btn-add-theory').disabled = !!(level?.theory);
}

function lcRenderTheory(levelId, topicId) {
    const resolvedTopicId = topicId ?? selectedTopicId;

    const numTopicId = Number(resolvedTopicId);
    const numLevelId = Number(levelId);

    const topic = ALL_TOPICS.find(t => t.id === numTopicId);
    const level = topic?.levels.find(l => l.id === numLevelId);
    const list = document.getElementById('theory-list');

    if (!level?.theory) {
        list.innerHTML = `<div class="lc-empty-state">
            <i class="fa fa-book-open"></i>
            <p>У этого уровня ещё нет теории</p></div>`;
        return;
    }

    const th = level.theory;
    const preview = th.content.length > 220
        ? th.content.substring(0, 220) + '…'
        : th.content;

    list.innerHTML = `
        <div class="lc-item lc-theory-card">
            <div class="lc-theory-header">
                <span class="lc-item-name">${lcEsc(th.title)}</span>
            </div>
            <p class="lc-theory-preview">${lcEsc(preview)}</p>
            <div class="lc-item-actions lc-theory-actions">
                <button class="lc-btn lc-btn--ghost lc-btn--sm" onclick="viewTheory()">
                    Просмотр
                </button>
                <button class="lc-btn lc-btn--ghost lc-btn--sm" onclick="editTheory()">
                    Редактировать
                </button>
                <button class="lc-btn lc-btn--danger lc-btn--sm" onclick="deleteTheory('${lcEscJs(th.title)}')">
                    Удалить
                </button>
            </div>
        </div>`;
}

function lcLockTheory() {
    selectedLevelId = null;
    document.getElementById('theory-level-label').textContent = 'Выберите уровень';
    document.getElementById('btn-add-theory').disabled = true;
    document.getElementById('col-theory').classList.add('lc-col--locked');
    document.getElementById('theory-list').innerHTML = `<div class="lc-empty-state">
        <i class="fa fa-book-open"></i>
        <p>Выберите уровень, чтобы управлять его теорией</p></div>`;
}

function viewTheory() {
    const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
    const level = topic?.levels.find(l => l.id === selectedLevelId);
    const th = level?.theory;
    if (!th) return;
    document.getElementById('view-theory-title').textContent = th.title;
    document.getElementById('view-theory-content').textContent = th.content;
    openModal('modal-view-theory');
}

function editTheory() {
    const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
    const level = topic?.levels.find(l => l.id === selectedLevelId);
    const th = level?.theory;
    if (!th) return;
    document.getElementById('theory-id').value = th.id;
    document.getElementById('theory-level-id').value = th.levelId;
    document.getElementById('theory-title').value = th.title;
    document.getElementById('theory-content').value = th.content;
    document.getElementById('modal-theory-title').textContent = 'Редактировать теорию';
    openModal('modal-theory');
}

async function saveTheory() {
    const id = parseInt(document.getElementById('theory-id').value);
    const levelId = document.getElementById('theory-level-id').value;
    const title = document.getElementById('theory-title').value.trim();
    const content = document.getElementById('theory-content').value.trim();

    if (!title) { lcShowToast('Введите заголовок теории', 'error'); return; }
    if (!content) { lcShowToast('Введите содержимое теории', 'error'); return; }

    const isNew = id === 0;
    const url = isNew ? '/admin/theory/create' : '/admin/theory/update';
    const data = isNew
        ? { LevelId: levelId, Title: title, Content: content }
        : { Id: id, Title: title, Content: content };

    try {
        const res = await lcPostForm(url, data);
        closeModal('modal-theory');
        lcShowToast(res.message);
        await lcReloadData();
    } catch (e) { lcShowToast(e.message, 'error'); }
}

function deleteTheory(title) {
    lcConfirmAction(`Удалить теорию «${title}»?`, async () => {
        const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
        const level = topic?.levels.find(l => l.id === selectedLevelId);
        const th = level?.theory;
        if (!th) return;
        try {
            const res = await lcPostForm(`/admin/theory/delete/${th.id}`, {});
            lcShowToast(res.message);
            await lcReloadData();
        } catch (e) { lcShowToast(e.message, 'error'); }
    });
}

async function lcReloadData() {
    try {
        const res = await fetch('/admin/learning', {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });
        const html = await res.text();
        const doc = new DOMParser().parseFromString(html, 'text/html');

        let fresh = null;
        doc.querySelectorAll('script').forEach(s => {
            const m = s.textContent.match(/const ALL_TOPICS = (\[[\s\S]*?\]);/);
            if (m) { try { fresh = JSON.parse(m[1]); } catch { } }
        });

        if (fresh) {
            ALL_TOPICS.length = 0;
            fresh.forEach(t => ALL_TOPICS.push(t));
        }

        const newList = doc.getElementById('topics-list');
        if (newList) {
            document.getElementById('topics-list').innerHTML = newList.innerHTML;
            document.getElementById('topics-count-label').textContent =
                ALL_TOPICS.length + ' шт.';
        }

        if (selectedTopicId) {
            const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
            if (topic) {
                lcRenderLevels(selectedTopicId);
            } else {
                selectedTopicId = null;
                lcLockLevels();
                lcLockTheory();
            }
        }

        if (selectedLevelId) {
            lcRenderTheory(selectedLevelId, selectedTopicId);
            const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
            const level = topic?.levels.find(l => l.id === selectedLevelId);
            document.getElementById('btn-add-theory').disabled = !!(level?.theory);
        }
    } catch {
        lcShowToast('Ошибка обновления данных', 'error');
    }
}

function lcInit() {
    document.getElementById('btn-add-topic').addEventListener('click', () => {
        document.getElementById('topic-id').value = '0';
        document.getElementById('topic-name').value = '';
        document.getElementById('topic-description').value = '';
        document.getElementById('topic-order').value = ALL_TOPICS.length;
        document.getElementById('modal-topic-title').textContent = 'Новая тема';
        openModal('modal-topic');
    });

    document.getElementById('btn-add-level').addEventListener('click', () => {
        if (!selectedTopicId) return;
        const topic = ALL_TOPICS.find(t => t.id === selectedTopicId);
        document.getElementById('level-id').value = '0';
        document.getElementById('level-name').value = '';
        document.getElementById('level-number').value = topic ? topic.levels.length + 1 : 1;
        lcPopulateTopicSelect(selectedTopicId);
        document.getElementById('modal-level-title').textContent = 'Новый уровень';
        openModal('modal-level');
    });

    document.getElementById('btn-add-theory').addEventListener('click', () => {
        if (!selectedLevelId) return;
        document.getElementById('theory-id').value = '0';
        document.getElementById('theory-level-id').value = selectedLevelId;
        document.getElementById('theory-title').value = '';
        document.getElementById('theory-content').value = '';
        document.getElementById('modal-theory-title').textContent = 'Добавить теорию';
        openModal('modal-theory');
    });

    document.querySelectorAll('.lc-modal-overlay').forEach(o =>
        o.addEventListener('click', e => { if (e.target === o) o.classList.remove('active'); })
    );
}