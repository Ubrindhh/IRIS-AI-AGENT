let sessionId = "";

const messagesContainer =
    document.getElementById("messages");

const messageInput =
    document.getElementById("messageInput");

const sendButton =
    document.getElementById("sendButton");

const typingIndicator =
    document.getElementById("typing");

const statusDot =
    document.getElementById("statusDot");

const statusText =
    document.getElementById("statusText");


document.addEventListener("DOMContentLoaded", () => {

    checkHealth();

    messageInput.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Enter" && !event.shiftKey) {

                event.preventDefault();

                sendMessage();
            }
        });

    messageInput.addEventListener(
        "input",
        autoResize);
});


async function checkHealth() {

    try {

        const response =
            await fetch("/health");

        if (!response.ok) {
            throw new Error("Health check failed.");
        }

        const result =
            await response.json();

        if (result.status === "healthy") {

            statusDot.classList.add("online");

            statusText.textContent =
                "AI Online";

        } else {

            statusDot.classList.add("offline");

            statusText.textContent =
                "AI Offline";
        }

    }
    catch {

        statusDot.classList.add("offline");

        statusText.textContent =
            "AI Offline";
    }
}


async function sendMessage() {

    const message =
        messageInput.value.trim();

    if (!message) {
        return;
    }

    addMessage(
        "user",
        message);

    messageInput.value = "";

    autoResize();

    setLoading(true);

    try {

        const response =
            await fetch(
                "/api/agent/chat",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body: JSON.stringify({
                        sessionId: sessionId,
                        message: message
                    })
                });

        const data =
            await response.json();

        if (!response.ok) {

            throw new Error(
                data.message ||
                "The AI service returned an error.");
        }

        sessionId =
            data.sessionId || sessionId;

        addMessage(
            "assistant",
            data.message || "No response received.",
            data);

    }
    catch (error) {

        addMessage(
            "assistant",
            "I could not process the request. " +
            "Please check that the AI Agent and Ollama are running.");
    }
    finally {

        setLoading(false);

        messageInput.focus();
    }
}


function addMessage(
    type,
    text,
    metadata = null) {

    const wrapper =
        document.createElement("div");

    wrapper.className =
        "message " + type;

    const avatar =
        document.createElement("div");

    avatar.className =
        "avatar";

    avatar.textContent =
        type === "user"
            ? "U"
            : "AI";

    const bubble =
        document.createElement("div");

    bubble.className =
        "bubble";

    const title =
        document.createElement("div");

    title.className =
        "message-title";

    title.textContent =
        type === "user"
            ? "You"
            : "IRIS AI";

    const content =
        document.createElement("div");

    content.textContent =
        text;

    bubble.appendChild(title);

    bubble.appendChild(content);

    if (
        type === "assistant" &&
        metadata &&
        metadata.toolUsed
    ) {

        const toolInfo =
            document.createElement("div");

        toolInfo.style.marginTop =
            "10px";

        toolInfo.style.fontSize =
            "11px";

        toolInfo.style.color =
            "#6b7280";

        toolInfo.textContent =
            "Tool used: " +
            metadata.toolName;

        bubble.appendChild(toolInfo);
    }

    wrapper.appendChild(avatar);

    wrapper.appendChild(bubble);

    messagesContainer.appendChild(wrapper);

    scrollToBottom();
}


function useExample(message) {

    messageInput.value =
        message;

    autoResize();

    messageInput.focus();
}


function setLoading(isLoading) {

    sendButton.disabled =
        isLoading;

    messageInput.disabled =
        isLoading;

    if (isLoading) {

        typingIndicator.classList.remove(
            "hidden");

    } else {

        typingIndicator.classList.add(
            "hidden");
    }

    scrollToBottom();
}


function scrollToBottom() {

    messagesContainer.scrollTop =
        messagesContainer.scrollHeight;
}


function autoResize() {

    messageInput.style.height =
        "auto";

    messageInput.style.height =
        Math.min(
            messageInput.scrollHeight,
            140) + "px";
}