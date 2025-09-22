window.msalHandleRedirect = async function () {
    try {
        const response = await msalInstance.handleRedirectPromise();

        if (response) {
            console.log("🔄 MSAL redirect response handled:", response);
        } else {
            console.log("ℹ️ No redirect response to handle.");
        }
    } catch (err) {
        console.error("❌ Error handling MSAL redirect:", err);

        // Failsafe: clear the interaction status manually to avoid being stuck
        sessionStorage.removeItem("msal.interaction.status");
    }
}

