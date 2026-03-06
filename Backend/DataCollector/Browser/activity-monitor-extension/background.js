console.log("extension loaded");

function sendUrl() {
    console.log("sendUrl called");

    browser.tabs.query({active: true, currentWindow: true}).then((tabs) => {
        console.log("tabs:", tabs);

        if (!tabs.length) return;

        fetch("http://127.0.0.1:8090/tab", {
            method: "POST",
            headers: {"Content-Type": "application/json"},
            body: JSON.stringify({url: tabs[0].url})
        }).then(r => console.log("sent"))
            .catch(e => console.error("fetch error", e));
    });
}

browser.tabs.onActivated.addListener(sendUrl);

browser.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
    if (changeInfo.status === "complete" && tab.active) {
        sendUrl();
    }
});