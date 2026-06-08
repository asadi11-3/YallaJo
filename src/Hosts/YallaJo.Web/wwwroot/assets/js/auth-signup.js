(function () {
    var input = document.getElementById('psw-input');
    var meter = document.getElementById('pswMeter');
    if (!input || !meter) return;
    input.addEventListener('input', function () {
        var v = input.value, score = 0;
        if (v.length >= 8) score++;
        if (/[a-z]/.test(v)) score++;
        if (/[A-Z]/.test(v)) score++;
        if (/[0-9]/.test(v)) score++;
        if (/[^A-Za-z0-9]/.test(v)) score++;
        var pct = (score / 5) * 100;
        var cls = score <= 2 ? 'bg-danger' : (score <= 4 ? 'bg-warning' : 'bg-success');
        meter.className = 'progress-bar ' + cls;
        meter.style.width = pct + '%';
        meter.setAttribute('aria-valuenow', pct);
    });
})();
