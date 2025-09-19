$(document).ready(function () {
    updateCartCount();

    
    // Add staggered animation to product cards
    const productCards = document.querySelectorAll('.product-card');
    productCards.forEach((card, index) => {
        card.style.animationDelay = `${index * 0.1}s`;
    });

    // Enhanced search form functionality
    const searchForm = document.getElementById('searchForm');
    const loadingContainer = document.querySelector('.loading-container');

    if (searchForm) {
        searchForm.addEventListener('submit', function () {
            if (loadingContainer) {
                loadingContainer.style.display = 'block';
            }
            const productsGrid = document.querySelector('.products-grid');
            if (productsGrid) {
                productsGrid.style.opacity = '0.5';
            }
        });
    }

    // Add smooth hover effects to search inputs
    const searchInputs = document.querySelectorAll('.search-input');
    searchInputs.forEach(input => {
        input.addEventListener('focus', function () {
            this.parentElement.style.transform = 'translateY(-2px)';
        });
        input.addEventListener('blur', function () {
            this.parentElement.style.transform = 'translateY(0)';
        });
    });

    // Search input enhancements
    const searchInputName = document.querySelector('input[name="search"]');
    const searchInputCategory = document.querySelector('input[name="category"]');

    if (searchInputName) {
        searchInputName.addEventListener('input', function () {
            this.style.borderColor = this.value.length > 0 ? '#48bb78' : '#e2e8f0';
        });
    }

    if (searchInputCategory) {
        searchInputCategory.addEventListener('input', function () {
            this.style.borderColor = this.value.length > 0 ? '#48bb78' : '#e2e8f0';
        });
    }

    // Image error handling
    const productImages = document.querySelectorAll('.product-image');
    productImages.forEach(img => {
        img.addEventListener('error', function () {
            this.style.display = 'none';
            const placeholder = this.parentElement.querySelector('.product-image-placeholder');
            if (placeholder) {
                placeholder.style.display = 'flex';
            }
        });
        img.addEventListener('load', function () {
            const placeholder = this.parentElement.querySelector('.product-image-placeholder');
            if (placeholder) {
                placeholder.style.display = 'none';
            }
        });
    });

    // Intersection Observer for scroll animations
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    productCards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(card);
    });

    // Quantity controls (event delegation for robustness)
    $(document).on('click', '.increase-btn', function () {
        const id = $(this).data('id');
        const maxStock = parseInt($(this).data('max'), 10) || 0;
        const qtySpan = $(`#qty-${id}`);
        let qty = parseInt(qtySpan.text(), 10) || 0;
        
        if (qty < maxStock) {
            qtySpan.text(qty + 1);
        } else {
            alert(`Only ${maxStock} items available in stock`);
        }
    });

    $(document).on('click', '.decrease-btn', function () {
        const id = $(this).data('id');
        const qtySpan = $(`#qty-${id}`);
        let qty = parseInt(qtySpan.text(), 10) || 0;
        if (qty > 1) {
            qtySpan.text(qty - 1);
        }
    });

    // Add to Cart with AJAX
    $(document).on('click', '.add-to-cart-btn', function () {
        const button = $(this);
        const id = button.data('id');
        const qty = parseInt($(`#qty-${id}`).text(), 10) || 1;  // ✅ get updated quantity from span
        const originalText = button.html();

        button.prop('disabled', true).html('<div class="loading-spinner"></div>');

        $.ajax({
            url: '/User/AddToCart',
            type: 'POST',
            data: { id: id, quantity: qty },  // ✅ send updated qty
            success: function (response) {
                if (response.success) {
                    button.html('✓ Added!');

                    // update cart count from server response (fresh value)
                    $('#cart-count').text(response.count);

                    // reset product qty to 1
                    $(`#qty-${id}`).text(1);

                    setTimeout(() => {
                        button.html(originalText).prop('disabled', false);
                    }, 1500);
                } else {
                    alert(response.message || 'Please login first.');
                    if (response.redirect) {
                        window.location.href = '/User/Login';
                    }
                    button.html(originalText).prop('disabled', false);
                }
            },
            error: function () {
                alert('Something went wrong.');
                button.html(originalText).prop('disabled', false);
            }
        });
    });


   
        // Function to update cart count
    function updateCartCount() {
        var url = (typeof getCartCountUrl !== 'undefined' && getCartCountUrl) ? getCartCountUrl : '/User/GetCartCount';
        $.ajax({
            url: url,
            type: 'GET',
            success: function (response) {
                $('#cart-count').text((response && response.count) ? response.count : 0);
            },
            error: function () {
                console.error("Failed to fetch cart count");
            }
        });
    }
});

  