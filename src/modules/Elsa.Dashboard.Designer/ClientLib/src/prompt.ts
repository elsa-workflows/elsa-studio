import Swal from 'sweetalert2';
export async function showPrompt(message) {
    await Swal.fire({
        title: 'Hi!',
        text: message,
        icon: 'info',
        confirmButtonText: 'Cool'
    });
}

