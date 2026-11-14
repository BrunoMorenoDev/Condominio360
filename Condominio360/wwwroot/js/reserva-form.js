// Condominio360 · Disponibilidad de un área comunal en una fecha.
// Solo informa al residente: el servidor vuelve a validar horario, cruces y morosidad al guardar.
(function () {
    'use strict';
    const caja = document.getElementById('disponibilidad');
    const area = document.getElementById('AreaId');
    const fecha = document.getElementById('Fecha');
    const texto = document.getElementById('disp-texto');
    const lista = document.getElementById('disp-lista');
    if (!caja || !area || !fecha) return;

    function item(contenido, clase) {
        const li = document.createElement('li');
        li.className = 'py-1';
        const span = document.createElement('span');
        span.className = clase || '';
        span.textContent = contenido;
        li.appendChild(span);
        return li;
    }

    async function actualizar() {
        lista.innerHTML = '';
        if (!area.value || area.value === '0' || !fecha.value) {
            texto.textContent = 'Elija un área y una fecha para ver los horarios ocupados.';
            return;
        }
        texto.textContent = 'Consultando…';
        try {
            const url = caja.dataset.url + '?areaId=' + encodeURIComponent(area.value) + '&fecha=' + encodeURIComponent(fecha.value);
            const r = await fetch(url, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' });
            if (!r.ok) throw new Error('HTTP ' + r.status);
            const d = await r.json();
            texto.textContent = 'Horario del área: ' + d.horario + ' · capacidad ' + d.capacidad + ' personas · costo ' + d.costo + '.';
            if (!d.ocupadas.length) {
                lista.appendChild(item('Todo el horario está libre ese día.', 'estado estado-ok'));
            } else {
                d.ocupadas.forEach(function (o) {
                    lista.appendChild(item('Ocupado ' + o.inicio + ' – ' + o.fin + ' (' + o.estado + ')', 'estado estado-pend'));
                });
            }
        } catch (e) {
            texto.textContent = 'No se pudo consultar la disponibilidad.';
        }
    }

    area.addEventListener('change', actualizar);
    fecha.addEventListener('change', actualizar);
    actualizar();
})();
