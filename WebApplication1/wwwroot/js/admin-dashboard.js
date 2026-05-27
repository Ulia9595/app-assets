window.addEventListener('offline', function () {
    window.location.href = '/static/admin-offline.html';
});

window.addEventListener('online', function () {
    if (window.location.pathname.includes('/admin/')) {
        location.reload();
    }
});