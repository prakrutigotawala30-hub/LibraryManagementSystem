$(document).ready(function () {

    var currentUrl = window.location.pathname;
    // Navbar active link highlight
    $(".nav-link").each(function () {
        if ($(this).attr("href") === currentUrl) {
            $(this).addClass("active-link");
        }
    });

    // Button click animation
    $(".btn-custom").click(function () {
        $(this).addClass("shadow-lg");
        setTimeout(() => {
            $(this).removeClass("shadow-lg");
        }, 200);
    });

    // Card hover animation (extra smooth)
    $(".custom-card").hover(
        function () {
            $(this).css("transform", "scale(1.05)");
        },
        function () {
            $(this).css("transform", "scale(1)");
        }
    );

});