// Condominio360 · Dropdowns en cascada (Torre → Unidad → Residente)
// Uso: <select data-cascada-destino="#UnidadId" data-cascada-url="/Catalogos/Unidades?torre=">
// Al cambiar el origen se piden al servidor (JSON) solo los registros que le pertenecen.
// El usuario nunca escribe una clave foránea: siempre la elige de la lista.
(function () {
    'use strict';

    function reiniciar(select, texto) {
        select.innerHTML = '';
        select.add(new Option(texto || select.dataset.vacio || 'Seleccione…', select.dataset.valorVacio ?? ''));
    }

    async function cargar(origen) {
        const destino = document.querySelector(origen.dataset.cascadaDestino);
        if (!destino) return;

        if (!origen.value || origen.value === (origen.dataset.valorVacio ?? '')) {
            reiniciar(destino);
            destino.disabled = true;
            destino.dispatchEvent(new Event('change'));
            return;
        }

        reiniciar(destino, 'Cargando…');
        destino.disabled = true;
        try {
            const respuesta = await fetch(origen.dataset.cascadaUrl + encodeURIComponent(origen.value), {
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin'
            });
            if (!respuesta.ok) throw new Error('HTTP ' + respuesta.status);
            const datos = await respuesta.json();

            reiniciar(destino, datos.length ? undefined : (destino.dataset.sinDatos || 'Sin registros'));
            datos.forEach(function (d) {
                const opcion = new Option(d.texto, d.id);
                if (d.responsable) opcion.dataset.responsable = 'true';
                destino.add(opcion);
            });
            destino.disabled = false;

            // Preselecciona al responsable de pago si el destino lo pide
            if (destino.dataset.autoResponsable !== undefined) {
                const responsable = Array.from(destino.options).find(function (o) { return o.dataset.responsable; });
                if (responsable) responsable.selected = true;
            }
        } catch (e) {
            reiniciar(destino, 'No se pudo cargar la lista');
        }
        destino.dispatchEvent(new Event('change'));
    }

    document.querySelectorAll('select[data-cascada-destino]').forEach(function (select) {
        select.addEventListener('change', function () { cargar(select); });
    });
})();
