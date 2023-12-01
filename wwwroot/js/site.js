// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Validación de formulario de ejemplo
function validarFormulario() {
    let x = document.forms["miFormulario"]["fname"].value;
    if (x == "") {
        alert("El nombre debe ser llenado");
        return false;
    }
}

// Agregar más funciones JavaScript según sea necesario
