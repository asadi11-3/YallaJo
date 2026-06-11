// External-auth interstitial: auto-submits the completion form shortly after
// load (the centralized _RecaptchaField partial binds to the form submit event
// and injects the token before letting the browser send it).
// PE1: a <noscript> submit button covers the no-JS path.
(function () {
    "use strict";

    if (document.documentElement.dataset.yjExternalCompleteWired === "1") { return; } // JS4
    document.documentElement.dataset.yjExternalCompleteWired = "1";

    setTimeout(function () {
        var form = document.getElementById("externalAuthCompleteForm");
        if (form) {
            form.requestSubmit();
        }
    }, 50);
})();
