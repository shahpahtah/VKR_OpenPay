(function () {
    function digitsOnly(value) {
        return /^\d+$/.test(value || "");
    }

    function setHint(element, message, isValid) {
        if (!element) return;
        element.textContent = message;
        element.classList.toggle("text-danger", !isValid);
        element.classList.toggle("text-success", isValid);
    }

    function validate() {
        const bic = document.querySelector(".js-bic")?.value?.trim() || "";
        const account = document.querySelector(".js-account")?.value?.trim() || "";
        const bicHint = document.querySelector(".js-bic-hint");
        const accountHint = document.querySelector(".js-account-hint");

        const bicValid = digitsOnly(bic) && bic.length === 9;
        setHint(bicHint, bicValid ? "БИК выглядит корректно." : "БИК должен содержать 9 цифр.", bicValid);

        const accountValid = digitsOnly(account) && account.length === 20;
        setHint(
            accountHint,
            accountValid ? "Номер счета содержит 20 цифр. Контрольная проверка выполняется на сервере." : "Счет должен содержать 20 цифр.",
            accountValid);
    }

    document.addEventListener("input", function (event) {
        if (event.target.matches(".js-bic, .js-account")) {
            validate();
        }
    });

    validate();
})();
