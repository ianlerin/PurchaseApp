window.msalLogout = async  function () {
    if (!window.msalInstance) {
        console.error("❌ MSAL instance not found!");
        return false;
    }

    // Guard against re-entrancy
    if (window.logoutInProgress) {
        console.warn("⚠️ Logout already in progress. Aborting duplicate attempt.");
        return false;
    }

    // Check MSAL internal interaction status
    const interactionStatus = sessionStorage.getItem("msal.interaction.status");
    if (interactionStatus === "inProgress") {
        console.warn("⏳ MSAL interaction already in progress. Logout will not continue.");
        return false;
    }

    const accounts = window.msalInstance.getAllAccounts();
    if (accounts.length === 0) {
        console.log("⚠️ No accounts found. Already logged out.");
        return true;
    }

    console.log(`🔑 Found ${accounts.length} account(s), logging out via redirect...`);

    try {
        window.logoutInProgress = true;

        await window.msalInstance.logoutRedirect({
            account: accounts[0],
            postLogoutRedirectUri: window.location.origin
        });

        // Technically the page is about to redirect, so this "true" isn't used
        return true;
    } catch (err) {
        console.error("❌ Logout redirect failed:", err);

        // Reset flag so user can retry logout
        window.logoutInProgress = false;

        return false;
    }
}


