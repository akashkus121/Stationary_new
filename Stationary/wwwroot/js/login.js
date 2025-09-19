document.addEventListener('DOMContentLoaded', function () {
    // Enhanced form submission with loading state
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function (e) {
            const loginBtn = document.getElementById('loginBtn');
            const originalContent = loginBtn.innerHTML;

            // Show loading state
            loginBtn.innerHTML = '<div class="loading-spinner"></div> Signing In...';
            loginBtn.disabled = true;

            // Note: In a real application, the form will submit normally
            // This is just for demo purposes to show the loading state
            // Remove this setTimeout in production
            setTimeout(function () {
                loginBtn.innerHTML = originalContent;
                loginBtn.disabled = false;
            }, 2000);
        });
    }

    // Add smooth focus animations
    const inputs = document.querySelectorAll('.form-control');
    inputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'translateY(-2px)';
        });

        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'translateY(0)';
        });
    });

    // Radio button animations
    const radioInputs = document.querySelectorAll('.radio-input');
    radioInputs.forEach(radio => {
        radio.addEventListener('change', function () {
            // Add a subtle animation when selection changes
            const roleSelection = document.querySelector('.role-selection');
            if (roleSelection) {
                roleSelection.style.transform = 'scale(1.02)';
                setTimeout(() => {
                    roleSelection.style.transform = 'scale(1)';
                }, 150);
            }
        });
    });
});