(function () {
    var dataElement = document.getElementById('auth-twofactor-data');
    var config = {};
    if (dataElement) {
        try { config = JSON.parse(dataElement.textContent || '{}'); } catch (e) { config = {}; }
    }

    var recaptchaEnabled = !!config.recaptchaEnabled;
    var recaptchaSiteKey = config.recaptchaSiteKey || '';
    var resendAction = config.resendAction || '';

    var btn = document.getElementById('resendBtn');
    var msg = document.getElementById('resendMsg');
    var emailInput = document.querySelector('[name="Email"]');
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');

    if (!btn || !msg) { return; }

    function getToken() {
        if (!recaptchaEnabled || !recaptchaSiteKey || typeof grecaptcha === 'undefined') {
            return Promise.resolve('');
        }
        return new Promise(function (resolve) {
            grecaptcha.ready(function () {
                grecaptcha.execute(recaptchaSiteKey, { action: resendAction })
                    .then(resolve)
                    .catch(function () { resolve(''); });
            });
        });
    }

    function disable(seconds) {
        btn.disabled = true;
        var remaining = seconds;
        var timer = setInterval(function () {
            remaining -= 1;
            if (remaining <= 0) {
                clearInterval(timer);
                btn.disabled = false;
                btn.textContent = 'Click to resend';
            } else {
                btn.textContent = 'Resend in ' + remaining + 's';
            }
        }, 1000);
    }

    btn.addEventListener('click', function () {
        msg.textContent = '';
        msg.className = 'small mb-3';

        getToken().then(function (token) {
            return fetch(btn.dataset.resendUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': tokenInput ? tokenInput.value : ''
                },
                body: JSON.stringify({
                    email: emailInput ? emailInput.value : '',
                    recaptchaToken: token
                })
            });
        }).then(function (res) {
            return res.json().then(function (data) { return { ok: res.ok, data: data }; });
        }).then(function (result) {
            if (result.ok) {
                msg.textContent = result.data.message || 'A new code has been sent.';
                msg.className = 'small mb-3 text-success';
                disable(60);
            } else {
                msg.textContent = result.data.error || 'Could not resend code.';
                msg.className = 'small mb-3 text-danger';
            }
        }).catch(function () {
            msg.textContent = 'Could not resend code. Please try again.';
            msg.className = 'small mb-3 text-danger';
        });
    });
})();
