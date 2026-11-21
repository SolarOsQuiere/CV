/*!
* Navegacion de la pagina unica: scroll suave a secciones y seccion activa (IntersectionObserver).
*/
window.cvNav = (() => {
    let observer = null;

    return {
        scrollTo(id) {
            const el = document.getElementById(id);
            if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        },
        observe(ids, dotnetRef) {
            this.unobserve();
            observer = new IntersectionObserver(entries => {
                entries.forEach(e => {
                    if (e.isIntersecting) dotnetRef.invokeMethodAsync('SetActive', e.target.id);
                });
            }, { rootMargin: '-40% 0px -55% 0px' });
            ids.forEach(id => {
                const el = document.getElementById(id);
                if (el) observer.observe(el);
            });
        },
        unobserve() {
            if (observer) { observer.disconnect(); observer = null; }
        }
    };
})();
