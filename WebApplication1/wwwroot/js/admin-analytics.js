'use strict';

let _anSelectedPlayer = null;
let _anDateFrom = '';
let _anDateTo = '';
let _anPvpTopic = '';

let _anChartElo = null;
let _anChartProgress = null;
let _anChartSolutions = null;
let _anChartPvp = null;

const AN_COLORS = {
    blue: 'rgba(37, 99, 235, 0.8)',
    blueB: 'rgba(37, 99, 235, 1)',
    green: 'rgba(22, 163, 74, 0.8)',
    greenB: 'rgba(22, 163, 74, 1)',
    red: 'rgba(220, 38, 38, 0.8)',
    redB: 'rgba(220, 38, 38, 1)',
};
const AN_FONT = "'Inter', 'Segoe UI', sans-serif";
Chart.defaults.font.family = AN_FONT;
Chart.defaults.color = '#64748b';

function anInit() {
    anInitFilters();
    anRenderAll();
}

function anInitFilters() {
    const today = new Date().toISOString().split('T')[0];
    const fromEl = document.getElementById('an-date-from');
    const toEl = document.getElementById('an-date-to');

    fromEl.max = today;
    toEl.max = today;

    fromEl.addEventListener('change', () => {
        _anDateFrom = fromEl.value;
        toEl.min = fromEl.value;
        anRenderProgress();
    });
    toEl.addEventListener('change', () => {
        _anDateTo = toEl.value;
        fromEl.max = toEl.value || today;
        anRenderProgress();
    });

    const sel = document.getElementById('an-pvp-topic');
    [...new Set(AN_PVP.map(p => p.topicName))].sort().forEach(t => {
        const opt = document.createElement('option');
        opt.value = t; opt.textContent = t;
        sel.appendChild(opt);
    });
    sel.addEventListener('change', () => {
        _anPvpTopic = sel.value;
        anRenderPvp();
    });

    anInitAutocomplete();
}

function anInitAutocomplete() {
    const input = document.getElementById('an-player-search');
    const dropdown = document.getElementById('an-player-dropdown');

    input.addEventListener('input', () => {
        const q = input.value.trim().toLowerCase();
        dropdown.innerHTML = '';

        if (!q) {
            dropdown.hidden = true;
            _anSelectedPlayer = null;
            anRenderAll();
            return;
        }

        const matches = AN_PLAYER_NAMES.filter(n => n.toLowerCase().includes(q));
        if (!matches.length) { dropdown.hidden = true; return; }

        matches.slice(0, 10).forEach(name => {
            const li = document.createElement('li');
            li.className = 'an-dropdown-item';
            li.textContent = name;
            li.addEventListener('click', () => {
                input.value = name;
                dropdown.hidden = true;
                _anSelectedPlayer = name;
                anRenderAll();
            });
            dropdown.appendChild(li);
        });
        dropdown.hidden = false;
    });

    document.addEventListener('click', e => {
        if (!input.contains(e.target) && !dropdown.contains(e.target))
            dropdown.hidden = true;
    });
}

function anResetFilters() {
    _anSelectedPlayer = null;
    _anDateFrom = '';
    _anDateTo = '';
    _anPvpTopic = '';

    const today = new Date().toISOString().split('T')[0];
    document.getElementById('an-player-search').value = '';
    document.getElementById('an-date-from').value = '';
    document.getElementById('an-date-from').max = today;
    document.getElementById('an-date-to').value = '';
    document.getElementById('an-date-to').min = '';
    document.getElementById('an-pvp-topic').value = '';

    anRenderAll();
}

function anGetProgressData() {
    if (_anSelectedPlayer) {
        const u = AN_PER_USER.find(x => x.displayName === _anSelectedPlayer);
        return u?.progress ?? [];
    }
    return AN_PROGRESS;
}

function anGetSolutionsData() {
    if (_anSelectedPlayer) {
        const u = AN_PER_USER.find(x => x.displayName === _anSelectedPlayer);
        return u
            ? { correct: u.solutionsCorrect, wrong: u.solutionsWrong }
            : { correct: 0, wrong: 0 };
    }
    return { correct: AN_CORRECT, wrong: AN_WRONG };
}

function anGetPvpData() {
    let data = _anSelectedPlayer
        ? (AN_PER_USER.find(x => x.displayName === _anSelectedPlayer)?.pvpStats ?? [])
        : AN_PVP;
    if (_anPvpTopic) data = data.filter(p => p.topicName === _anPvpTopic);
    return data;
}

function anFilterByDate(data) {
    return data.filter(p => {
        const okFrom = !_anDateFrom || p.month >= _anDateFrom.slice(0, 7);
        const okTo = !_anDateTo || p.month <= _anDateTo.slice(0, 7);
        return okFrom && okTo;
    });
}

function anRenderAll() {
    anRenderElo();
    anRenderProgress();
    anRenderSolutions();
    anRenderPvp();
}

function anDestroyChart(ref) {
    if (ref) { ref.destroy(); }
    return null;
}

function anRenderElo() {
    const ctx = document.getElementById('an-chart-elo');
    if (!ctx || !AN_ELO.length) return;
    _anChartElo = anDestroyChart(_anChartElo);

    _anChartElo = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: AN_ELO.map(x => x.label),
            datasets: [{
                label: 'Игроков',
                data: AN_ELO.map(x => x.count),
                backgroundColor: AN_COLORS.blue,
                borderColor: AN_COLORS.blueB,
                borderWidth: 1,
                borderRadius: 6,
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: {
                y: { beginAtZero: true, ticks: { stepSize: 1 }, grid: { color: 'rgba(0,0,0,0.05)' } },
                x: { grid: { display: false } }
            }
        }
    });
}

function anRenderProgress() {
    const ctx = document.getElementById('an-chart-progress');
    if (!ctx) return;
    _anChartProgress = anDestroyChart(_anChartProgress);

    const filtered = anFilterByDate(anGetProgressData());
    if (!filtered.length) {
        anShowEmpty(ctx, 'Данных за выбранный период нет');
        return;
    }

    const months = ['Янв', 'Фев', 'Мар', 'Апр', 'Май', 'Июн', 'Июл', 'Авг', 'Сен', 'Окт', 'Ноя', 'Дек'];
    const labels = filtered.map(p => {
        const [y, m] = p.month.split('-');
        return `${months[parseInt(m) - 1]} ${y}`;
    });

    _anChartProgress = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: 'Тем завершено',
                    data: filtered.map(p => p.topicsCompleted),
                    borderColor: AN_COLORS.blueB,
                    backgroundColor: 'rgba(37,99,235,0.1)',
                    tension: 0.3, fill: true, pointRadius: 4,
                    pointBackgroundColor: AN_COLORS.blueB,
                },
                {
                    label: 'Заданий решено',
                    data: filtered.map(p => p.levelsCompleted),
                    borderColor: AN_COLORS.greenB,
                    backgroundColor: 'rgba(22,163,74,0.08)',
                    tension: 0.3, fill: true, pointRadius: 4,
                    pointBackgroundColor: AN_COLORS.greenB,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top', labels: { boxWidth: 12, font: { size: 12 } } } },
            scales: {
                y: { beginAtZero: true, ticks: { stepSize: 1 }, grid: { color: 'rgba(0,0,0,0.05)' } },
                x: { grid: { display: false } }
            }
        }
    });
}

function anRenderSolutions() {
    const ctx = document.getElementById('an-chart-solutions');
    if (!ctx) return;
    _anChartSolutions = anDestroyChart(_anChartSolutions);

    const { correct, wrong } = anGetSolutionsData();
    document.getElementById('an-legend-correct').textContent = `Правильно: ${correct}`;
    document.getElementById('an-legend-wrong').textContent = `Неправильно: ${wrong}`;

    if (correct === 0 && wrong === 0) {
        anShowEmpty(ctx, 'Решений пока нет');
        return;
    }

    _anChartSolutions = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Правильно', 'Неправильно'],
            datasets: [{
                data: [correct, wrong],
                backgroundColor: [AN_COLORS.green, AN_COLORS.red],
                borderColor: ['#fff', '#fff'],
                borderWidth: 3,
                hoverOffset: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '68%',
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: c => {
                            const total = correct + wrong;
                            const pct = total ? Math.round(c.parsed / total * 100) : 0;
                            return `${c.parsed} (${pct}%)`;
                        }
                    }
                }
            }
        }
    });
}

function anRenderPvp() {
    const ctx = document.getElementById('an-chart-pvp');
    if (!ctx) return;
    _anChartPvp = anDestroyChart(_anChartPvp);

    const data = anGetPvpData();
    if (!data.length) {
        anShowEmpty(ctx, 'Нет данных для выбранного фильтра');
        return;
    }

    _anChartPvp = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.map(p => `#${p.tournamentId} ${p.topicName}`),
            datasets: [
                {
                    label: 'Правильные',
                    data: data.map(p => p.correctAnswers),
                    backgroundColor: AN_COLORS.green,
                    borderColor: AN_COLORS.greenB,
                    borderWidth: 1, borderRadius: 4,
                },
                {
                    label: 'Неправильные',
                    data: data.map(p => p.wrongAnswers),
                    backgroundColor: AN_COLORS.red,
                    borderColor: AN_COLORS.redB,
                    borderWidth: 1, borderRadius: 4,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top', labels: { boxWidth: 12, font: { size: 12 } } } },
            scales: {
                x: { stacked: false, grid: { display: false }, ticks: { maxRotation: 30, font: { size: 11 } } },
                y: { beginAtZero: true, ticks: { stepSize: 1 }, grid: { color: 'rgba(0,0,0,0.05)' } }
            }
        }
    });
}

function anShowEmpty(canvas, text) {
    const ctx2d = canvas.getContext('2d');
    canvas.height = 120;
    ctx2d.clearRect(0, 0, canvas.width, canvas.height);
    ctx2d.fillStyle = '#94a3b8';
    ctx2d.font = `14px ${AN_FONT}`;
    ctx2d.textAlign = 'center';
    ctx2d.fillText(text, canvas.width / 2, 60);
}