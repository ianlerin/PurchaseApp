window.msalLogout = async function () {
    if (!window.msalInstance) {
        console.error("❌ MSAL instance not found!");
        return;
    }

    try {
        console.log("✅ MSAL instance found!");
        const accounts = window.msalInstance.getAllAccounts();

        if (accounts.length > 0) {
            console.log(`🔑 Found ${accounts.length} account(s), logging out...`);

            // Perform popup logout
            await window.msalInstance.logoutPopup({
                account: accounts[0]
            });

            // Just in case, clear remaining cached accounts manually
            const tokenCache = window.msalInstance.getTokenCache();
            for (const acc of accounts) {
                await tokenCache.removeAccount(acc);
                console.log(`🧹 Removed account: ${acc.username}`);
            }

            console.log("✅ Successfully logged out (popup + cache cleared).");
        } else {
            console.log("⚠️ No accounts found. Already logged out.");
        }
    } catch (err) {
        console.error("MSAL logout error:", err);
    }
};