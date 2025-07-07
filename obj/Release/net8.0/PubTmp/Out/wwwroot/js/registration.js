document.addEventListener("DOMContentLoaded", function () {
    const empInput = document.querySelector('input[name="EmployeeId"]');

    if (!empInput) {
        console.error("EmployeeId input not found in form.");
        return;
    }

    empInput.addEventListener("change", function () {
        const empId = this.value.trim().toUpperCase();
        this.value = empId;

        if (!empId) return;

        const baseUrl = window.appBaseUrl || '/';
        const fetchUrl = `${baseUrl}Employee/GetEmployeeById?id=${encodeURIComponent(empId)}`;

        fetch(fetchUrl)
            .then(response => {
                if (!response.ok) throw new Error("Employee not found.");
                return response.json();
            })
            .then(data => {
                document.querySelector('input[name="FirstName"]').value = data.firstName || "";
                document.querySelector('input[name="LastName"]').value = data.lastName || "";
                document.querySelector('input[name="Email"]').value = data.email || "";
                document.querySelector('input[name="Section"]').value = data.section || "";
            })
            .catch(error => {
                alert("Employee not found.");
                document.querySelector('input[name="FirstName"]').value = "";
                document.querySelector('input[name="LastName"]').value = "";
                document.querySelector('input[name="Email"]').value = "";
                document.querySelector('input[name="Section"]').value = "";
                console.error("Fetch error:", error);
            });
    });


    //Password Confirmation/Validation
    const form = document.querySelector("form");
    const password = document.getElementById("password");
    const confirmPassword = document.getElementById("confirmPassword");
    const confirmError = confirmPassword.nextElementSibling; // span.text-danger after confirmPassword

    form.addEventListener("submit", function (e) {
        if (password.value !== confirmPassword.value) {
            e.preventDefault(); // stop form submission
            confirmError.textContent = "Passwords do not match.";
            confirmPassword.classList.add("is-invalid");
        } else {
            confirmError.textContent = "";
            confirmPassword.classList.remove("is-invalid");
        }
    });

    //Clear error when user starts typing again
    confirmPassword.addEventListener("input", function () {
        if (password.value === confirmPassword.value) {
            confirmError.textContent = "";
            confirmPassword.classList.remove("is-invalid");
        }
    });
});

//Redirect to login after registration
function showSuccessModalAndRedirect() {
    var modalElement = document.getElementById('successModal');
    var modal = new bootstrap.Modal(modalElement);
    modal.show();

    setTimeout(function () {
        window.location.href = '/Home/Login'; 
    }, 4000);
}


$(document).ready(function () {
    $("#registerForm").submit(function (e) {
        e.preventDefault();

        $.ajax({
            type: "POST",
            url: "/Home/Register", 
            data: $(this).serialize(),
            success: function (response) {
                showSuccessModalAndRedirect();
            },
            error: function (xhr) {
                alert("Registration failed");
                console.error(xhr.responseText);
            }
        });
    });
});