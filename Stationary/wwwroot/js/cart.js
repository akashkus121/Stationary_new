

// Increase quantity
$(document).on('click', '.increase-btn', function () {
    const id = $(this).data('id');
    const maxStock = parseInt($(this).data('max'), 10) || 0;
    const qtySpan = $(`#qty-${id}`);
    let qty = parseInt(qtySpan.text());
    
    if (qty < maxStock) {
        updateCartQuantity(id, qty + 1, qtySpan);
    } else {
        alert(`Only ${maxStock} items available in stock`);
    }
});

// Decrease quantity
$(document).on('click', '.decrease-btn', function () {
    const id = $(this).data('id');
    const qtySpan = $(`#qty-${id}`);
    let qty = parseInt(qtySpan.text());
    if (qty > 1) {
        updateCartQuantity(id, qty - 1, qtySpan);
    }
});

// Remove item
$(document).on('click', '.remove-btn', function () {
    const id = $(this).data('id');
    $.ajax({
        url: '/User/RemoveFromCart',
        type: 'POST',
        data: { id: id },
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
                if (response.redirect) {
                    window.location.href = '/User/Login';
                }
            }
        },
        error: function () {
            alert('Something went wrong.');
        }
    });
});

// Update cart quantity
function updateCartQuantity(id, quantity, qtySpan) {
    $.ajax({
        url: '/User/UpdateCartQuantity',
        type: 'POST',
        data: { id: id, quantity: quantity },
        success: function (response) {
            if (response.success) {
                qtySpan.text(quantity);
                updateCartCount();
                updateCartTotals();
            } else {
                alert(response.message);
                if (response.redirect) {
                    window.location.href = '/User/Login';
                }
            }
        },
        error: function () {
            alert('Something went wrong.');
        }
    });
}

// ✅ Update cart count
function updateCartCount() {
    $.ajax({
        url: '/User/GetCartCount',
        type: 'GET',
        success: function (response) {
            $('#cart-count').text(response.count || 0);
        },
        error: function () {
            console.error('Failed to update cart count.');
        }
    });
};

// Payment method selection
$(document).ready(function() {
    // Handle payment method selection
    $('input[name="paymentMethod"]').change(function() {
        const selectedMethod = $(this).val();
        $('#selected-payment-method').val(selectedMethod);
        
        if (selectedMethod === 'upi') {
            $('#upi-section').slideDown(300);
            generateQRCode();
        } else {
            $('#upi-section').slideUp(300);
        }
    });
    
    // Generate initial QR code if UPI is selected
    if ($('#upi-payment').is(':checked')) {
        $('#upi-section').show();
        generateQRCode();
    }
});

// Generate QR Code for UPI payment
function generateQRCode() {
    const grandTotal = $('#grand-total').text().replace(/[^\d.]/g, '');
    const upiId = '908akashkushwaha@okaxis';
    const payeeName = 'Akash Stationery';
    const currency = 'INR';

    const upiDeepLink = `upi://pay?pa=${upiId}&pn=${encodeURIComponent(payeeName)}&am=${grandTotal}&cu=${currency}`;

    // Clear old QR
    $('#qr-code').empty();

    // Generate QR code
    new QRCode(document.getElementById("qr-code"), {
        text: upiDeepLink,
        width: 200,
        height: 200,
        colorDark: "#000000",
        colorLight: "#ffffff"
    });

    // Update UPI amount display
    $('#upi-amount').text(`₹${grandTotal}`);
}

// Open UPI app
function openUPIApp() {
    const grandTotal = $('#grand-total').text().replace('$', '');
    const upiId = '908akashkushwaha@okaxis';
    const payeeName = 'Akash Stationery';
    const currency = 'INR';
    
    const upiDeepLink = `upi://pay?pa=${upiId}&pn=${encodeURIComponent(payeeName)}&am=${grandTotal}&cu=${currency}`;
    
    // Try to open UPI app
    window.location.href = upiDeepLink;
    
    // Fallback: Show UPI details
    setTimeout(function() {
        alert(`UPI Payment Details:\n\nUPI ID: ${upiId}\nName: ${payeeName}\nAmount: ₹${grandTotal}\n\nPlease use any UPI app to make the payment.`);
    }, 1000);
}

// Update cart totals when quantity changes
function updateCartTotals() {
    let subtotal = 0;
    let totalItems = 0;

    $('.cart-item').each(function () {
        const quantity = parseInt($(this).find('.quantity').text());
        const price = parseFloat($(this).find('.cart-item-price').text().replace('$', ''));
        const itemTotal = quantity * price;

        subtotal += itemTotal;
        totalItems += quantity;
    });

    // Get discount from input
    const discount = parseFloat($('#discount').val()) || 0;

    // Calculate final grand total
    let grandTotal = subtotal - discount;
    if (grandTotal < 0) grandTotal = 0; // Prevent negative totals

    // Update UI
    $('#total-items').text(totalItems);
    $('#subtotal').text('$' + subtotal.toFixed(2));
    $('#grand-total').text('$' + grandTotal.toFixed(2));
    $('#upi-amount').text('₹' + grandTotal.toFixed(2));

    // Regenerate QR code if UPI is selected
    if ($('#upi-payment').is(':checked')) {
        generateQRCode();
    }
}

// Trigger update when discount changes
$(document).on('input', '#discount', function () {
    updateCartTotals();
});
