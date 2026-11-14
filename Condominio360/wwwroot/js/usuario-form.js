// Condominio360 · Formulario de usuarios: muestra el vínculo con el residente solo para el rol Residente.
// Es solo ayuda visual: el servidor exige y valida el vínculo según el rol.
(function () {
    'use strict';
    const rol = document.getElementById('RolId');
    const bloque = document.getElementById('bloque-residente');
    if (!rol || !bloque) return;

    function actualizar() {
        const opcion = rol.options[rol.selectedIndex];
        bloque.hidden = !opcion || opcion.text.trim() !== 'Residente';
    }

    rol.addEventListener('change', actualizar);
    actualizar();
})();
