(function ($) {
    "use strict";
    var busy = false;
    var region = $("#cart-region");
    if (!region.length) { return; }
    var error = $("#cart-network-error");
    function render(html) {
        // Only accept the server's encoded Razor cart fragment, never generic error pages.
        var nodes = $.parseHTML(html, document, false);
        var snapshot = $(nodes).filter("section[data-cart-snapshot='true']");
        if (snapshot.length !== 1) { return false; }
        region.empty().append(snapshot);
        $("[data-global-cart-count]").text(snapshot.attr("data-count"));
        $("[data-global-cart-total]").text(snapshot.attr("data-total"));
        return true;
    }
    function reportError() {
        error.text("No se pudo confirmar la operación. Recarga el carrito para revisar su estado antes de intentarlo nuevamente.").prop("hidden", false);
    }
    region.on("submit", "form.cart-form", function (event) {
        event.preventDefault();
        if (busy) { return; }
        var form = $(this);
        if (!this.checkValidity()) { this.reportValidity(); return; }
        var confirmation = form.attr("data-confirm");
        if (confirmation && !window.confirm(confirmation)) { return; }
        var data = form.serialize();
        busy = true;
        error.prop("hidden", true).text("");
        region.attr("aria-busy", "true").find("button").prop("disabled", true);
        $.ajax({ url: form.attr("action"), method: "POST", data: data, dataType: "html", timeout: 15000 })
            .done(function (html) { if (!render(html)) { reportError(); } })
            .fail(function (xhr) { if (!xhr.responseText || !render(xhr.responseText)) { reportError(); } })
            .always(function () {
                busy = false;
                region.attr("aria-busy", "false").find("button").prop("disabled", false);
            });
    });
}(jQuery));
