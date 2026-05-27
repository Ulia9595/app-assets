'use strict';

function upApplyFilters() {
    const search = document.getElementById('up-search').value.trim().toLowerCase();
    const dateFrom = document.getElementById('up-date-from').value;
    const dateTo = document.getElementById('up-date-to').value;

    const rows = document.querySelectorAll('#up-table-body tr');
    let visible = 0;

    rows.forEach(row => {
        const name = row.dataset.name ?? '';
        const date = row.dataset.date ?? '';

        const matchName = !search || name.includes(search);
        const matchFrom = !dateFrom || date >= dateFrom;
        const matchTo = !dateTo || date <= dateTo;

        const show = matchName && matchFrom && matchTo;
        row.style.display = show ? '' : 'none';
        if (show) visible++;
    });

    document.getElementById('up-count-label').textContent = visible;
}

function upResetFilters() {
    document.getElementById('up-search').value = '';
    document.getElementById('up-date-from').value = '';
    document.getElementById('up-date-to').value = '';
    upApplyFilters();
}

function upInit() {
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('up-date-from').max = today;
    document.getElementById('up-date-to').max = today;

    document.getElementById('up-date-from').addEventListener('change', function () {
        document.getElementById('up-date-to').min = this.value;
        upApplyFilters();
    });

    document.getElementById('up-date-to').addEventListener('change', function () {
        document.getElementById('up-date-from').max = this.value || today;
        upApplyFilters();
    });

    document.getElementById('up-search').addEventListener('input', upApplyFilters);
}