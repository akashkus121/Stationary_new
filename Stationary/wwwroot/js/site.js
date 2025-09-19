// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function updateCartCount() {
    $.ajax({
        url: getCartCountUrl,
        type: 'GET',
        success: function (response) {
            $('#cart-count').text(response.count || 0);
        },
        error: function () {
            console.error("Failed to fetch cart count");
        }
    });
}
