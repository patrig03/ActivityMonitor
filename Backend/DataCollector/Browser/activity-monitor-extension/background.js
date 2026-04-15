console.log("Activity Monitor - Firefox extension loaded");

let lastUrl = null;
let lastSentUrl = null;
let retryCount = 0;
const MAX_RETRIES = 3;

function sendUrl() {
    console.log("sendUrl called");

    browser.tabs.query({active: true, currentWindow: true}).then((tabs) => {
        console.log("tabs:", tabs);

        if (!tabs.length) {
            console.log("No active tabs found");
            return;
        }

        const currentUrl = tabs[0].url;

        // Skip internal browser pages
        if (currentUrl.startsWith("about:") || currentUrl.startsWith("moz-extension:")) {
            console.log("Skipping internal page:", currentUrl);
            return;
        }

        // Only send if URL has changed
        if (currentUrl === lastUrl) {
            console.log("URL unchanged, skipping");
            return;
        }

        lastUrl = currentUrl;

        sendUrlWithRetry(currentUrl);
    }).catch(e => console.error("Error querying tabs:", e));
}

function sendUrlWithRetry(url, attempt = 0) {
    if (url !== lastUrl) {
        // URL changed while we were retrying
        console.log("URL changed during retry, aborting");
        return;
    }

    fetch("http://127.0.0.1:8090/tab", {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({url: url})
    })
    .then(response => {
        if (response.ok) {
            console.log("Successfully sent to port 8090:", url);
            lastSentUrl = url;
            retryCount = 0;
        } else {
            console.error("Server responded with error:", response.status);
            if (attempt < MAX_RETRIES) {
                setTimeout(() => sendUrlWithRetry(url, attempt + 1), 1000 * (attempt + 1));
            }
        }
    })
    .catch(e => {
        console.error("Fetch error:", e);
        if (attempt < MAX_RETRIES) {
            console.log(`Retrying (${attempt + 1}/${MAX_RETRIES})...`);
            setTimeout(() => sendUrlWithRetry(url, attempt + 1), 1000 * (attempt + 1));
        } else {
            console.error("Max retries reached, giving up");
        }
    });
}

// Listen for tab activation (switching between tabs)
browser.tabs.onActivated.addListener((activeInfo) => {
    sendUrl();
});

// Listen for tab updates (URL changes, page loads)
browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === "complete" && tab.active) {
        sendUrl();
    }
});

// Listen for window focus changes
browser.windows.onFocusChanged.addListener((windowId) => {
    if (windowId !== browser.windows.WINDOW_ID_NONE) {
        sendUrl();
    }
});

// Send initial URL on startup
browser.runtime.onStartup.addListener(() => {
    sendUrl();
});

// Send URL when extension is installed/updated
browser.runtime.onInstalled.addListener(() => {
    sendUrl();
});