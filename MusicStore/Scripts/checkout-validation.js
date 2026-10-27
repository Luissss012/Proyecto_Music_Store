(function ($) {
    "use strict";
    $.validator.addMethod("checkoutpattern", function (value, element, pattern) {
        if (this.optional(element)) return true;
        try {
            var match = new RegExp("^(?:" + pattern + ")$", "u").exec(value);
            return match !== null && match[0].length === value.length;
        } catch (error) {
            // Older browsers without Unicode properties defer to server validation.
            return true;
        }
    });
    $.validator.unobtrusive.adapters.addSingleVal("checkoutpattern", "pattern");
}(jQuery));
