/*!
* Navegacion del carrusel horizontal: scroll a secciones, seccion activa y teclado.
*/
window.cvNav = (() => {
    let observer = null;
    let keyHandler = null;

    const carousel = () => document.getElementById('cv-carousel');

    return {
        scrollTo(id) {
            const el = document.getElementById(id);
            if (!el) return;
            const c = carousel();
            if (c) c.scrollTo({ left: el.offsetLeft, behavior: 'smooth' });
            else el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        },
        observe(ids, dotnetRef) {
            this.unobserve();
            const c = carousel();
            observer = new IntersectionObserver(entries => {
                entries.forEach(e => {
                    if (e.isIntersecting) dotnetRef.invokeMethodAsync('SetActive', e.target.id);
                });
            }, c ? { root: c, threshold: 0.6 } : { rootMargin: '-40% 0px -55% 0px' });
            ids.forEach(id => {
                const el = document.getElementById(id);
                if (el) observer.observe(el);
            });

            keyHandler = ev => {
                const tag = (ev.target && ev.target.tagName) || '';
                if (/INPUT|TEXTAREA|SELECT/.test(tag) || ev.altKey || ev.ctrlKey || ev.metaKey) return;
                if (ev.key === 'ArrowRight') { ev.preventDefault(); dotnetRef.invokeMethodAsync('Step', 1); }
                else if (ev.key === 'ArrowLeft') { ev.preventDefault(); dotnetRef.invokeMethodAsync('Step', -1); }
            };
            document.addEventListener('keydown', keyHandler);
        },
        unobserve() {
            if (observer) { observer.disconnect(); observer = null; }
            if (keyHandler) { document.removeEventListener('keydown', keyHandler); keyHandler = null; }
        }
    };
})();
