const STORAGE_KEY = "iris-agent-session-v1";

let sessionId = "";
let transcript = [];
let loadingTimer = null;

const messagesContainer = document.getElementById("messages");
const messageInput = document.getElementById("messageInput");
const sendButton = document.getElementById("sendButton");
const typingIndicator = document.getElementById("typing");
const typingLabel = typingIndicator.querySelector("label");
const statusDot = document.getElementById("statusDot");
const statusText = document.getElementById("statusText");
const welcomeTpl = document.getElementById("welcomeTpl");
const newSessionButton = document.getElementById("newSessionButton");

const logsButton = document.getElementById("logsButton");
const logsBadge = document.getElementById("logsBadge");
const logsPanel = document.getElementById("logsPanel");
const logsScrim = document.getElementById("logsScrim");
const logsList = document.getElementById("logsList");
const logsMeta = document.getElementById("logsMeta");

let logsSeen = 0;

const TYPING_DEFAULT = "IRIS AI is working on it";
const TYPING_SLOW = "Still working — a detailed question can take up to a minute";


document.addEventListener("DOMContentLoaded", () => {

    checkHealth();
    restoreSession();

    messageInput.addEventListener("keydown", (event) => {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            sendMessage();
        }
    });

    messageInput.addEventListener("input", autoResize);

    newSessionButton.addEventListener("click", newSession);
    logsButton.addEventListener("click", openLogs);
    logsScrim.addEventListener("click", closeLogs);
    document.getElementById("logsClose").addEventListener("click", closeLogs);
    document.getElementById("logsRefresh").addEventListener("click", () => fetchLogs(true));
    document.getElementById("logsClear").addEventListener("click", clearLogs);
    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && !logsPanel.classList.contains("hidden")) {
            closeLogs();
        }
    });

    refreshLogsBadge(true);
    setInterval(() => refreshLogsBadge(false), 20000);
});


/* ---------- session persistence ---------- */

function persist() {
    try {
        localStorage.setItem(
            STORAGE_KEY,
            JSON.stringify({ sessionId, transcript }));
    } catch {
        /* storage unavailable - run in-memory only */
    }
}

function restoreSession() {
    let saved = null;

    try {
        saved = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
    } catch {
        saved = null;
    }

    if (saved && Array.isArray(saved.transcript) && saved.transcript.length) {
        sessionId = saved.sessionId || "";
        transcript = saved.transcript;
        messagesContainer.innerHTML = "";
        for (const m of transcript) {
            renderMessage(m.type, m.text, m.meta || null, m.ts, true);
        }
        jumpToBottom();
        return;
    }

    showWelcome();
}

function showWelcome() {
    messagesContainer.innerHTML = "";
    if (welcomeTpl && welcomeTpl.content) {
        messagesContainer.appendChild(welcomeTpl.content.cloneNode(true));
    }
}

function newSession() {
    if (transcript.length && !confirm("Start a new session? The current conversation will be cleared.")) {
        return;
    }
    sessionId = "";
    transcript = [];
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* ignore */ }
    showWelcome();
    messageInput.value = "";
    autoResize();
    messageInput.focus();
}


/* ---------- health ---------- */

async function checkHealth() {
    try {
        const response = await fetch("/health");
        const result = response.ok ? await response.json() : null;

        if (result && result.status === "healthy") {
            statusDot.classList.remove("offline");
            statusDot.classList.add("online");
            statusText.textContent = "AI Online";
        } else {
            statusDot.classList.remove("online");
            statusDot.classList.add("offline");
            statusText.textContent = "AI Offline";
        }
    } catch {
        statusDot.classList.remove("online");
        statusDot.classList.add("offline");
        statusText.textContent = "AI Offline";
    }
}


/* ---------- sending ---------- */

async function sendMessage() {

    const message = messageInput.value.trim();
    if (!message || sendButton.disabled) {
        return;
    }

    addMessage("user", message);
    messageInput.value = "";
    autoResize();
    setLoading(true);

    try {
        const response = await fetch("/api/agent/chat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ sessionId, message })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || "The AI service returned an error.");
        }

        sessionId = data.sessionId || sessionId;
        addMessage("assistant", data.message || "No response received.", data);

    } catch {
        addMessage(
            "assistant",
            "I could not process the request. Please check that the AI Agent and Ollama are running. Open Diagnostics for details.");
    } finally {
        setLoading(false);
        messageInput.focus();
        refreshLogsBadge(false);
    }
}


/* ---------- messages ---------- */

function addMessage(type, text, metadata = null) {
    const ts = Date.now();
    transcript.push({ type, text, meta: metadata, ts });
    persist();
    renderMessage(type, text, metadata, ts, false);
}

function renderMessage(type, text, metadata, ts, restoring) {

    const wrapper = document.createElement("div");
    wrapper.className = "message " + type;

    const avatar = document.createElement("div");
    avatar.className = "avatar";
    avatar.setAttribute("aria-hidden", "true");
    avatar.textContent = type === "user" ? "U" : "IA";

    const bubble = document.createElement("div");
    bubble.className = "bubble";

    const titleRow = document.createElement("div");
    titleRow.className = "message-title";

    const who = document.createElement("span");
    who.textContent = type === "user" ? "You" : "IRIS AI";
    titleRow.appendChild(who);

    if (ts) {
        const time = document.createElement("span");
        time.className = "msg-time";
        time.textContent = formatTime(ts);
        titleRow.appendChild(time);
    }

    const content = document.createElement("div");
    content.className = "bubble-body";
    renderContent(content, text);

    bubble.appendChild(titleRow);
    bubble.appendChild(content);

    if (type === "assistant") {

        if (isReviewPrompt(text)) {
            bubble.classList.add("bubble--review");
            bubble.appendChild(buildReviewActions());
        }

        bubble.appendChild(buildCopyButton(text));

        if (metadata && metadata.toolUsed && metadata.toolName) {
            const src = document.createElement("div");
            src.className = "tool-note";
            src.textContent = "Source: IRIS " + prettyTool(metadata.toolName);
            bubble.appendChild(src);
        }
    }

    wrapper.appendChild(avatar);
    wrapper.appendChild(bubble);
    messagesContainer.appendChild(wrapper);

    if (!restoring) {
        scrollToBottom();
    }
}

function prettyTool(name) {
    const map = {
        get_customer: "customer record",
        search_customers: "customer search",
        get_customer_accounts: "accounts",
        get_products: "card products",
        get_customer_cards: "customer cards",
        get_card: "card record",
        get_branches: "branch list",
        get_currencies: "currency list",
        create_card: "card service"
    };
    return map[name] || "data service";
}

function isReviewPrompt(text) {
    const t = String(text || "").toLowerCase();
    return t.includes("pending debit-card request")
        && t.includes("cancel");
}

function buildReviewActions() {
    const row = document.createElement("div");
    row.className = "review-actions";

    const confirm = document.createElement("button");
    confirm.type = "button";
    confirm.className = "ra ra-confirm";
    confirm.textContent = "Confirm & create";

    const cancel = document.createElement("button");
    cancel.type = "button";
    cancel.className = "ra ra-cancel";
    cancel.textContent = "Cancel";

    const lock = () => {
        confirm.disabled = true;
        cancel.disabled = true;
    };

    confirm.addEventListener("click", () => { lock(); quickSend("yes"); });
    cancel.addEventListener("click", () => { lock(); quickSend("cancel"); });

    row.appendChild(confirm);
    row.appendChild(cancel);
    return row;
}

function buildCopyButton(text) {
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "copy-msg";
    btn.title = "Copy";
    btn.setAttribute("aria-label", "Copy message");
    btn.textContent = "Copy";

    btn.addEventListener("click", async () => {
        try {
            await navigator.clipboard.writeText(String(text || ""));
            btn.textContent = "Copied";
            btn.classList.add("done");
            setTimeout(() => {
                btn.textContent = "Copy";
                btn.classList.remove("done");
            }, 1400);
        } catch {
            /* clipboard blocked */
        }
    });

    return btn;
}

function quickSend(message) {
    if (sendButton.disabled) {
        return;
    }
    messageInput.value = message;
    sendMessage();
}


/* ---------- quick actions ---------- */

function useExample(message, autoSend) {
    if (sendButton.disabled) {
        return;
    }
    messageInput.value = message;
    autoResize();
    messageInput.focus();
    if (autoSend) {
        sendMessage();
    }
}


/* ---------- content rendering ---------- */

function renderContent(container, text) {

    const lines = String(text == null ? "" : text).split("\n");
    let list = null;

    const flushList = () => {
        if (list) {
            container.appendChild(list);
            list = null;
        }
    };

    for (const raw of lines) {

        const line = raw.trimEnd();
        const bullet = line.match(/^\s*[-•]\s+(.*)$/);

        if (bullet) {
            if (!list) {
                list = document.createElement("ul");
            }
            list.appendChild(makeItem(bullet[1]));
            continue;
        }

        flushList();

        if (line.trim() === "") {
            const spacer = document.createElement("div");
            spacer.className = "line-gap";
            container.appendChild(spacer);
            continue;
        }

        const kv = line.match(/^([A-Za-z][A-Za-z /()'-]{1,28}):\s*(.+)$/);
        const row = document.createElement("div");

        if (kv) {
            const label = document.createElement("strong");
            label.textContent = kv[1] + ": ";
            row.appendChild(label);
            row.appendChild(makeValue(kv[2]));
        } else {
            row.textContent = line;
        }

        container.appendChild(row);
    }

    flushList();
}

function makeItem(textValue) {
    const li = document.createElement("li");
    li.appendChild(makeValue(textValue));
    return li;
}

/* Wrap the value so identifiers/numbers can be click-to-copied. */
function makeValue(textValue) {
    const frag = document.createDocumentFragment();
    const parts = String(textValue).split(/(\b[0-9][0-9*\-]{4,}\b)/);

    parts.forEach((part, i) => {
        if (i % 2 === 1) {
            const chip = document.createElement("span");
            chip.className = "copyable";
            chip.title = "Click to copy";
            chip.textContent = part;
            chip.addEventListener("click", () => copyInline(chip, part));
            frag.appendChild(chip);
        } else if (part) {
            frag.appendChild(document.createTextNode(part));
        }
    });

    return frag;
}

async function copyInline(el, value) {
    try {
        await navigator.clipboard.writeText(value);
        el.classList.add("copied");
        setTimeout(() => el.classList.remove("copied"), 900);
    } catch {
        /* clipboard blocked */
    }
}


/* ---------- loading / util ---------- */

function setLoading(isLoading) {

    sendButton.disabled = isLoading;
    document.body.classList.toggle("busy", isLoading);

    if (isLoading) {
        typingLabel.textContent = TYPING_DEFAULT;
        typingIndicator.classList.remove("hidden");
        clearTimeout(loadingTimer);
        loadingTimer = setTimeout(() => {
            typingLabel.textContent = TYPING_SLOW;
        }, 4000);
    } else {
        clearTimeout(loadingTimer);
        typingIndicator.classList.add("hidden");
    }

    scrollToBottom();
}

function scrollToBottom() {
    messagesContainer.scrollTo({
        top: messagesContainer.scrollHeight,
        behavior: "smooth"
    });
}

function jumpToBottom() {
    messagesContainer.scrollTop = messagesContainer.scrollHeight;
}

function formatTime(ts) {
    try {
        return new Date(ts).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    } catch {
        return "";
    }
}

function autoResize() {
    messageInput.style.height = "auto";
    messageInput.style.height = Math.min(messageInput.scrollHeight, 140) + "px";
}


/* ---------- diagnostics log ---------- */

async function refreshLogsBadge(isInitial) {
    try {
        const res = await fetch("/api/logs");
        if (!res.ok) return;
        const data = await res.json();
        const count = data.count || 0;

        if (isInitial) {
            logsSeen = count;
        }

        const unseen = Math.max(0, count - logsSeen);
        if (unseen > 0) {
            logsBadge.textContent = unseen > 99 ? "99+" : String(unseen);
            logsBadge.classList.remove("hidden");
        } else {
            logsBadge.classList.add("hidden");
        }
    } catch {
        /* diagnostics endpoint unavailable */
    }
}

function openLogs() {
    logsPanel.classList.remove("hidden");
    logsPanel.setAttribute("aria-hidden", "false");
    logsScrim.classList.remove("hidden");
    fetchLogs(false);
}

function closeLogs() {
    logsPanel.classList.add("hidden");
    logsPanel.setAttribute("aria-hidden", "true");
    logsScrim.classList.add("hidden");
}

async function fetchLogs(announce) {
    logsList.innerHTML = "";
    const loading = document.createElement("div");
    loading.className = "logs-empty";
    loading.textContent = "Loading…";
    logsList.appendChild(loading);

    try {
        const res = await fetch("/api/logs");
        const data = res.ok ? await res.json() : { count: 0, entries: [] };
        renderLogs(data.entries || []);

        logsSeen = data.count || 0;
        logsBadge.classList.add("hidden");

        const n = (data.entries || []).length;
        logsMeta.textContent = n
            ? n + " event" + (n === 1 ? "" : "s") + " (newest first, max 200)"
            : "No events recorded";

        if (announce && !n) {
            logsMeta.textContent = "No events recorded — nothing has gone wrong.";
        }
    } catch {
        renderLogs([]);
        logsMeta.textContent = "Could not load diagnostics.";
    }
}

function renderLogs(entries) {
    logsList.innerHTML = "";

    if (!entries.length) {
        const empty = document.createElement("div");
        empty.className = "logs-empty";
        empty.textContent = "No issues or errors recorded.";
        logsList.appendChild(empty);
        return;
    }

    for (const e of entries) {
        const level = (e.level || "info").toLowerCase();

        const entry = document.createElement("div");
        entry.className = "logs-entry " + level;

        const row = document.createElement("div");
        row.className = "row";

        const lvl = document.createElement("span");
        lvl.className = "lvl";
        lvl.textContent = level;

        const src = document.createElement("span");
        src.className = "src";
        src.textContent = e.source || "";

        const msg = document.createElement("span");
        msg.className = "msg";
        msg.textContent = e.message || "";

        const ts = document.createElement("span");
        ts.className = "ts";
        ts.textContent = formatLogTime(e.timestampUtc);

        row.appendChild(lvl);
        row.appendChild(src);
        row.appendChild(msg);
        row.appendChild(ts);
        entry.appendChild(row);

        if (e.detail) {
            const detail = document.createElement("pre");
            detail.className = "detail";
            detail.textContent = e.detail;
            entry.appendChild(detail);
            row.addEventListener("click", () => entry.classList.toggle("open"));
            row.title = "Click to show details";
        }

        logsList.appendChild(entry);
    }
}

async function clearLogs() {
    try {
        await fetch("/api/logs/clear", { method: "POST" });
    } catch {
        /* ignore */
    }
    logsSeen = 0;
    fetchLogs(false);
}

function formatLogTime(iso) {
    try {
        const d = new Date(iso);
        return d.toLocaleString([], {
            month: "short", day: "numeric",
            hour: "2-digit", minute: "2-digit", second: "2-digit"
        });
    } catch {
        return "";
    }
}
