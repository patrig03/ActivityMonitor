console.log("Activity Monitor - Chromium extension loaded");

let lastUrl = null;
let lastSentUrl = null;
let retryCount = 0;
const MAX_RETRIES = 3;

function sendUrl() {
    console.log("sendUrl called");

    chrome.tabs.query({active: true, currentWindow: true}, (tabs) => {
        console.log("tabs:", tabs);

        if (!tabs || !tabs.length) {
            console.log("No active tabs found");
            return;
        }

        const currentUrl = tabs[0].url;

        // Skip internal browser pages
        if (currentUrl.startsWith("chrome:") ||
            currentUrl.startsWith("chrome-extension:") ||
            currentUrl.startsWith("edge:") ||
            currentUrl.startsWith("about:")) {
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
    });
}

function sendUrlWithRetry(url, attempt = 0) {
    if (url !== lastUrl) {
        // URL changed while we were retrying
        console.log("URL changed during retry, aborting");
        return;
    }

    fetch("http://127.0.0.1:8091/tab", {
        method: "POST",
        headers: {"Content-Type": "application/json"},
        body: JSON.stringify({url: url})
    })
    .then(response => {
        if (response.ok) {
            console.log("Successfully sent to port 8091:", url);
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
chrome.tabs.onActivated.addListener((activeInfo) => {
    sendUrl();
});

// Listen for tab updates (URL changes, page loads)
chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === "complete" && tab.active) {
        sendUrl();
    }
});

// Listen for window focus changes
chrome.windows.onFocusChanged.addListener((windowId) => {
    if (windowId !== chrome.windows.WINDOW_ID_NONE) {
        sendUrl();
    }
});

// Send initial URL on startup
chrome.runtime.onStartup.addListener(() => {
    sendUrl();
});

// Send URL when extension is installed/updated
chrome.runtime.onInstalled.addListener(() => {
    sendUrl();
});
