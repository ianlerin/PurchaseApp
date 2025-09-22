window.initMsal = function (clientId, authority) {
    console.log("🔑 Initializing MSAL instance...");

    const config = {
        auth: {
            clientId: clientId,
            authority: authority,
            redirectUri: window.location.origin
        },
        cache: {
            cacheLocation: "sessionStorage", 
            storeAuthStateInCookie: true
        }
    };

    window.msalInstance = new msal.PublicClientApplication(config);
    console.log("✅ MSAL instance created.");
};

