$.validator.addMethod("minage", function(value, element, params) {
    if (!value) {
        return true;
    }

    var requiredMinAge = parseInt(params.age, 10);

    // yyyy-mm-dd
    var dob = new Date(value);

    if (isNaN(dob.getTime())) {
        return false;
    }

    var today = new Date();
    var age = today.getFullYear() - dob.getFullYear();

    // Adjust age if the user's birthday hasnt happened (yet)
    var monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
        age--;
    }

    return age >= requiredMinAge;
});

$.validator.unobtrusive.adapters.add("minage", ["age"], function(options) {
    options.rules.minage = {
        age: options.params.age
    };
    options.messages["minage"] = options.message;
});