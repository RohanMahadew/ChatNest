function initDateInput(inputId, minAge) {
    if (!Number.isInteger(minAge) || minAge < 0) throw new TypeError("minAge must be a positive integer");

    const input = document.getElementById(inputId);
    if (input == null) throw new Error(`Input element with id "${inputId}" not found`);

    const type = input.type;

    if (type === "date") {
        const today = new Date();

        const formatDate = (date) =>
            date.toISOString().split("T")[0];

        const minDate = new Date(today);
        minDate.setFullYear(today.getFullYear() - 100);

        const maxDate = new Date(today);
        maxDate.setFullYear(today.getFullYear() - 16);

        input.min = formatDate(minDate);
        input.max = formatDate(maxDate);
        input.value = formatDate(maxDate);
    }
}