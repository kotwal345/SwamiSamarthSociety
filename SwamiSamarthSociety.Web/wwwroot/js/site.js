// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    var toggle = document.getElementById('sidebarToggle');
    var close = document.getElementById('sidebarClose');
    var backdrop = document.getElementById('sidebarBackdrop');
    if (!toggle) return;

    function setOpen(open) {
        document.body.classList.toggle('sidebar-open', open);
        toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    toggle.addEventListener('click', function () {
        setOpen(!document.body.classList.contains('sidebar-open'));
    });
    if (close) close.addEventListener('click', function () { setOpen(false); });
    if (backdrop) backdrop.addEventListener('click', function () { setOpen(false); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') setOpen(false);
    });
})();
