document.getElementById('reservationForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const data = {
        EmployeeId: document.getElementById('employeeId').value,
        FirstName: document.getElementById('firstName').value,
        LastName: document.getElementById('lastName').value,
        Section: document.getElementById('section').value,
        CustomerType: document.getElementById('costumerType').value,
        ReservationDate: document.getElementById('selectedDate').value,
        MealTime: document.getElementById('time').value,
        MenuType: document.getElementById('menu').value,
        Quantity: parseInt(document.getElementById('quantity').value)
    };

    const token = document.querySelector('[name="__RequestVerificationToken"]').value;

    fetch('../Reservation/SaveAdvanceOrder', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(data)
    })
        .then(res => res.json())
        .then(res => {
            if (res.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Reserved!',
                    text: res.message,
                }).then(() => {
                    document.getElementById('reservationForm').reset();
                });
            } else {
                console.error(res.errors || res.message);
                Swal.fire('Error', res.message, 'error');
            }
        })
        .catch(err => {
            console.error(err);
            Swal.fire('Request failed', err.message, 'error');
        });
});
