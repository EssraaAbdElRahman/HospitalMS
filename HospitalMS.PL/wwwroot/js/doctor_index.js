document.getElementById('deleteModal').addEventListener('show.bs.modal', function (event) {
    const button = event.relatedTarget;
    document.getElementById('deleteDoctorId').value = button.getAttribute('data-id');
    document.getElementById('deleteDoctorName').textContent = button.getAttribute('data-name');
});

setTimeout(function () {
    ['successMessage', 'errorMessage'].forEach(function (id) {
        const el = document.getElementById(id);
        if (el) {
            el.style.transition = 'opacity 0.5s';
            el.style.opacity = '0';
            setTimeout(() => el.remove(), 500);
        }
    });
}, 2000);