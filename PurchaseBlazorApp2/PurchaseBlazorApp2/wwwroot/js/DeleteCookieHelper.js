window.deleteCookie = function (name) {
    document.cookie = name + "=; path=/; max-age=0;";
};