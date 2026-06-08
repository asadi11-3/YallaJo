// The centralized _RecaptchaField partial binds to the form submit
// event and injects the token before letting the browser send it.
// Auto-submit after a short delay so users don't have to click Continue
// when JavaScript is enabled.
setTimeout(function () {
    var f = document.getElementById('externalAuthCompleteForm');
    if (f) f.requestSubmit();
}, 50);
