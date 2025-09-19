function handleImageError(img) {
    img.src = 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="60" height="60" viewBox="0 0 60 60"%3E%3Crect width="60" height="60" fill="%23ecf0f1"/%3E%3Ctext x="30" y="35" text-anchor="middle" fill="%237f8c8d" font-size="12"%3ENo Image%3C/text%3E%3C/svg%3E';
    img.alt = 'Image not available';
}

// Confirm delete action
function confirmDelete() {
    return confirm('Are you sure you want to delete this product? This action cannot be undone.');
}

// Add loading state to buttons when clicked
document.addEventListener('DOMContentLoaded', function () {
    // Add loading state to action buttons
    const actionButtons = document.querySelectorAll('.action-btn');
    actionButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            if (!this.classList.contains('delete-btn') || confirmDelete()) {
                this.style.opacity = '0.7';
                this.style.pointerEvents = 'none';

                // Add loading spinner
                const icon = this.querySelector('i');
                if (icon) {
                    icon.className = 'fas fa-spinner fa-spin';
                }
            }
        });
    });

    // Add hover effects to table rows
    const tableRows = document.querySelectorAll('.products-table tbody tr');
    tableRows.forEach(row => {
        row.addEventListener('mouseenter', function () {
            this.style.transform = 'scale(1.01)';
        });

        row.addEventListener('mouseleave', function () {
            this.style.transform = 'scale(1)';
        });
    });

    // Add click animation to buttons
    const buttons = document.querySelectorAll('.btn-primary, .action-btn');
    buttons.forEach(button => {
        button.addEventListener('mousedown', function () {
            this.style.transform = 'scale(0.98)';
        });

        button.addEventListener('mouseup', function () {
            this.style.transform = 'scale(1)';
        });
    });
});

// Search functionality (if you want to add a search feature later)
function searchProducts(query) {
    const rows = document.querySelectorAll('.products-table tbody tr');
    const searchTerm = query.toLowerCase();

    rows.forEach(row => {
        const productName = row.querySelector('.product-name').textContent.toLowerCase();
        const productCategory = row.querySelector('.product-category').textContent.toLowerCase();

        if (productName.includes(searchTerm) || productCategory.includes(searchTerm)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    });
}

// Utility function to show notifications (if you want to add notifications)
function showNotification(message, type = 'success') {
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 20px;
        border-radius: 8px;
        color: white;
        font-weight: 500;
        z-index: 1000;
        transform: translateX(100%);
        transition: transform 0.3s ease;
        ${type === 'success' ? 'background: #27ae60;' : 'background: #e74c3c;'}
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.transform = 'translateX(0)';
    }, 100);

    setTimeout(() => {
        notification.style.transform = 'translateX(100%)';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}