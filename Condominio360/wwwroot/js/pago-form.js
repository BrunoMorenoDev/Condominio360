// Condominio360 · Ayudas del formulario de pago.
// Solo mejora la experiencia: el servidor vuelve a validar referencia y saldo al guardar.
(function () {
    'use strict';
    const metodo = document.getElementById('Metodo');
    const referencia = document.getElementById('Referencia');
    const ayudaReferencia = document.getElementById('ayuda-referencia');
    const unidad = document.getElementById('UnidadId');
    const fecha = document.getElementById('FechaPago');
    const saldo = document.getElementById('saldo-unidad');

    function actualizarReferencia() {
        if (!metodo || !referencia) return;
        const efectivo = metodo.value === 'Efectivo';
        if (ayudaReferencia) {
            ayudaReferencia.textContent = efectivo
                ? 'Opcional para pagos en efectivo (n.º de recibo).'
                : 'Obligatoria: número de la transferencia o del depósito.';
        }
    }

    async function actualizarSaldo() {
        if (!saldo || !unidad) return;
        const id = parseInt(unidad.value, 10);
        if (!id) { saldo.textContent = '—'; return; }
        saldo.textContent = 'Calculando…';
        try {
            const url = saldo.dataset.url + '?unidadId=' + id + '&fecha=' + encodeURIComponent(fecha ? fecha.value : '');
            const respuesta = await fetch(url, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' });
            const datos = await respuesta.json();
            saldo.textContent = datos.texto;
        } catch (e) {
            saldo.textContent = 'No disponible';
        }
    }

    metodo && metodo.addEventListener('change', actualizarReferencia);
    unidad && unidad.addEventListener('change', actualizarSaldo);
    fecha && fecha.addEventListener('change', actualizarSaldo);
    actualizarReferencia();
    actualizarSaldo();
})();
