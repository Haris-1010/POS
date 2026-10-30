$(document).ready(function () {
    let chatHistory = [];

    // Initialize chatbot UI
    function initChatbot() {
        const $chatContainer = $('#chatbot-container');
        const $chatBody = $('#botui-app');

        // Clear previous messages
        $chatBody.empty();

        // Add welcome message, matching the header's EN/Urdu toggle (window.CoreMedixI18n)
        var lang = (window.CoreMedixI18n && window.CoreMedixI18n.getMode()) || 'en';
        var welcomeMessage = lang === 'ur'
            ? '<span dir="rtl" class="cm-i18n-ur">السلام علیکم! 👋 میں آپ کا POS اسسٹنٹ ہوں۔<br><br>' +
              'پوچھیں: "کتنا اسٹاک باقی ہے"، "آج کی سیلز"، "کسٹمر علی"<br><br>' +
              'آپ کو کیسی مدد چاہیے؟</span>'
            : 'Assalam-o-Alaikum! 👋 I am your POS Assistant.<br><br>' +
              'Ask me things like: "low stock report", "today sales", "customer ali"<br><br>' +
              'How can I help you?';
        addBotMessage(welcomeMessage);

        // Show chat container
        $chatContainer.show();
        $('#chatbot-toggle').hide();

        // Scroll to bottom
        scrollToBottom();
    }

    // Add bot message to chat
    function addBotMessage(message) {
        const $chatBody = $('#botui-app');
        const timestamp = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });

        const messageHtml = `
            <div class="bot-message">
                <div class="bot-bubble">
                    <div>${message}</div>
                    <div class="message-time">${timestamp}</div>
                </div>
            </div>
        `;
        $chatBody.append(messageHtml);
        scrollToBottom();
    }

    // Add user message to chat
    function addUserMessage(message) {
        const $chatBody = $('#botui-app');
        const timestamp = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });

        const messageHtml = `
            <div class="user-message">
                <div class="user-bubble">
                    <div>${message}</div>
                    <div class="message-time">${timestamp}</div>
                </div>
            </div>
        `;
        $chatBody.append(messageHtml);
        scrollToBottom();
    }

    // Scroll to bottom of chat
    function scrollToBottom() {
        const $chatBody = $('#botui-app');
        $chatBody.scrollTop($chatBody[0].scrollHeight);
    }

    // Escape HTML to prevent XSS
    function escapeHtml(unsafe) {
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    // Send message to server
    function sendMessage(message) {
        if (!message.trim()) return;

        // Add user message to chat
        addUserMessage(message);

        // Clear input
        $('#chatbot-input').val('');

        // Show typing indicator
        const $chatBody = $('#botui-app');
        const typingHtml = `
            <div class="typing-indicator">
                <div class="typing-dots">
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        `;
        $chatBody.append(typingHtml);
        scrollToBottom();

        // Send AJAX request
        $.ajax({
            url: '/Chatbot/QueryAI',
            method: 'POST',
            data: {
                prompt: message,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                // Remove typing indicator
                $chatBody.find('.typing-indicator').remove();

                if (response.Status) {
                    // Format the response with line breaks
                    const formattedResponse = response.Message.replace(/\n/g, '<br>');
                    addBotMessage(formattedResponse);
                } else {
                    addBotMessage('Maazrat, request process nahi ho sakin. Dobara try karein.<br>Sorry, I could not process your request. Please try again.');
                }
            },
            error: function (xhr, status, error) {
                // Remove typing indicator
                $chatBody.find('.typing-indicator').remove();

                addBotMessage('Server se connection nahi ho raha. Internet check karke dobara try karein.<br>Error connecting to the server. Please check your internet connection and try again.');
                console.error('Chatbot error:', error);
            }
        });
    }

    // Event handlers
    $('#chatbot-toggle').click(function () {
        initChatbot();
    });

    $('.js-close-chatbot').click(function () {
        $('#chatbot-container').hide();
        $('#chatbot-toggle').show();
    });

    $('.js-send-message').click(function () {
        const userInput = $('#chatbot-input').val().trim();
        sendMessage(userInput);
    });

    $('#chatbot-input').keypress(function (e) {
        if (e.which === 13) { // Enter key
            const userInput = $(this).val().trim();
            sendMessage(userInput);
        }
    });

    // Auto-focus on input when chatbot opens
    $(document).on('shown.bs.collapse', '#chatbot-container', function () {
        $('#chatbot-input').focus();
    });
});