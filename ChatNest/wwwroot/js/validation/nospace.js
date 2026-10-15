$.validator.addMethod("nospace",
    function (value, element, parameters) {
        // If it's empty, let [Required] handle it.
        if (!value) {
            console.log("NoSpace.js: !Value");
            return true;
        }

        //return value.indexOf(' ') === -1;
        console.log(element.id + ":" + !value.includes(' '));
        return !value.includes(' ');
    }
);

// Wire the unobtrusive adapter
$.validator.unobtrusive.adapters.add("nospace", [], function (options) {
    options.rules.nospace = {};
    options.messages["nospace"] = options.message;
});