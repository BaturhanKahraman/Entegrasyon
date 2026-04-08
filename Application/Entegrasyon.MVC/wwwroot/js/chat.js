// ── Chat Page (SignalR Real-time) ─────────────────────────────────────

(function () {
    var container = document.getElementById('chat-container');
    if (!container) return;

    var conversationId = container.dataset.conversationId;
    var currentUserId = container.dataset.currentUserId;
    var csrfToken = document.querySelector('meta[name="csrf-token"]');

    // ── SignalR Connection ────────────────────────────────────────────

    if (typeof signalR === 'undefined') {
        console.warn('SignalR not loaded');
        return;
    }

    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/ChatHub')
        .withAutomaticReconnect()
        .build();

    // Receive new messages in the active conversation
    connection.on('ReceiveMessage', function (message) {
        var area = document.getElementById('message-area');
        if (!area) return;

        var isOwn = message.senderId === currentUserId;
        appendMessage(area, message.senderName || '', message.content, isOwn);
    });

    // Typing indicator
    connection.on('UserTyping', function (convId, userId, isTyping) {
        if (convId.toString() !== conversationId) return;
        var indicator = document.getElementById('typing-indicator');
        if (indicator) {
            indicator.style.display = isTyping ? '' : 'none';
        }
    });

    connection.start().then(function () {
        if (conversationId) {
            connection.invoke('JoinConversation', parseInt(conversationId));
        }
    }).catch(function (err) {
        console.warn('ChatHub connection failed:', err);
    });

    // ── Reply State ─────────────────────────────────────────────────

    var replyToMessageId = null;

    window.setReply = function (messageId, senderName, content) {
        replyToMessageId = messageId;
        var preview = document.getElementById('reply-preview');
        if (preview) {
            preview.textContent = '';
            var row = document.createElement('div');
            row.className = 'd-flex align-items-center justify-content-between bg-light rounded p-2 mb-2';
            var info = document.createElement('div');
            info.className = 'text-truncate';
            var strong = document.createElement('strong');
            strong.className = 'small';
            strong.textContent = senderName;
            var br = document.createElement('br');
            var span = document.createElement('span');
            span.className = 'small text-secondary';
            span.textContent = content.substring(0, 60);
            info.appendChild(strong);
            info.appendChild(br);
            info.appendChild(span);
            var closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'btn-close btn-close-sm ms-2';
            closeBtn.addEventListener('click', function () { clearReply(); });
            row.appendChild(info);
            row.appendChild(closeBtn);
            preview.appendChild(row);
            preview.style.display = '';
        }
        var input = document.getElementById('chat-input');
        if (input) input.focus();
    };

    window.clearReply = function () {
        replyToMessageId = null;
        var preview = document.getElementById('reply-preview');
        if (preview) { preview.textContent = ''; preview.style.display = 'none'; }
    };

    // ── Send Message ─────────────────────────────────────────────────

    var form = document.getElementById('chat-form');
    var input = document.getElementById('chat-input');

    if (form && input) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            var content = input.value.trim();
            if (!content || !conversationId) return;

            var payload = {
                conversationId: parseInt(conversationId),
                content: content
            };
            if (replyToMessageId) {
                payload.replyToMessageId = replyToMessageId;
            }

            fetch('/chat/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken ? csrfToken.content : ''
                },
                body: JSON.stringify(payload)
            }).then(function (response) {
                if (!response.ok) {
                    console.error('Failed to send message:', response.status);
                }
            });

            input.value = '';
            clearReply();
        });

        // Typing indicator: notify others when typing
        var typingTimeout = null;
        input.addEventListener('input', function () {
            if (!conversationId) return;

            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('SendTyping', parseInt(conversationId), true);

                clearTimeout(typingTimeout);
                typingTimeout = setTimeout(function () {
                    connection.invoke('SendTyping', parseInt(conversationId), false);
                }, 2000);
            }
        });
    }

    // ── Scroll to Bottom on Load ─────────────────────────────────────

    var messageArea = document.getElementById('message-area');
    if (messageArea) {
        messageArea.scrollTop = messageArea.scrollHeight;
    }

    // ── New Chat Modal ───────────────────────────────────────────────

    var btnNewChat = document.getElementById('btn-new-chat');
    if (btnNewChat) {
        btnNewChat.addEventListener('click', function () {
            var modal = new bootstrap.Modal(document.getElementById('new-chat-modal'));

            fetch('/chat/users', {
                headers: {
                    'RequestVerificationToken': csrfToken ? csrfToken.content : ''
                }
            })
                .then(function (r) { return r.json(); })
                .then(function (users) {
                    var list = document.getElementById('user-list');
                    while (list.firstChild) list.removeChild(list.firstChild);

                    if (users.length === 0) {
                        var empty = document.createElement('div');
                        empty.className = 'text-center text-secondary p-3';
                        empty.textContent = 'Kullanici bulunamadi';
                        list.appendChild(empty);
                        return;
                    }

                    users.forEach(function (user) {
                        var item = document.createElement('a');
                        item.href = '#';
                        item.className = 'list-group-item list-group-item-action d-flex align-items-center';

                        var avatar = document.createElement('span');
                        avatar.className = 'avatar avatar-sm me-2 bg-primary-lt';
                        avatar.textContent = user.name.charAt(0).toUpperCase();

                        var nameSpan = document.createElement('span');
                        nameSpan.textContent = user.name;

                        item.appendChild(avatar);
                        item.appendChild(nameSpan);

                        item.addEventListener('click', function (e) {
                            e.preventDefault();
                            fetch('/chat/new', {
                                method: 'POST',
                                headers: {
                                    'Content-Type': 'application/json',
                                    'RequestVerificationToken': csrfToken ? csrfToken.content : ''
                                },
                                body: JSON.stringify({ targetUserId: user.id })
                            })
                                .then(function (r) { return r.json(); })
                                .then(function (data) {
                                    window.location.href = '/chat/' + data.conversationId;
                                });
                        });

                        list.appendChild(item);
                    });
                });

            modal.show();
        });
    }

    // ── DOM Helpers ──────────────────────────────────────────────────

    function appendMessage(area, senderName, content, isOwn, messageId) {
        var wrapper = document.createElement('div');
        wrapper.className = 'd-flex mb-3 ' + (isOwn ? 'justify-content-end' : 'justify-content-start');

        var bubble = document.createElement('div');
        bubble.className = (isOwn ? 'bg-primary text-white' : 'bg-light') + ' rounded-3 p-2 px-3 position-relative chat-bubble';
        bubble.style.maxWidth = '70%';

        if (!isOwn && senderName) {
            var nameEl = document.createElement('div');
            nameEl.className = 'fw-bold small mb-1';
            nameEl.textContent = senderName;
            bubble.appendChild(nameEl);
        }

        var contentEl = document.createElement('div');
        contentEl.textContent = content;
        bubble.appendChild(contentEl);

        var now = new Date();
        var timeStr = now.getHours().toString().padStart(2, '0') + ':' + now.getMinutes().toString().padStart(2, '0');
        var timeEl = document.createElement('div');
        timeEl.className = (isOwn ? 'text-white-50' : 'text-secondary') + ' small text-end mt-1';
        timeEl.textContent = timeStr;
        bubble.appendChild(timeEl);

        // Reply button (show on hover)
        if (messageId) {
            var replyBtn = document.createElement('button');
            replyBtn.className = 'btn btn-sm btn-ghost-secondary chat-reply-btn';
            replyBtn.title = 'Yanitla';
            replyBtn.style.cssText = 'position:absolute;top:2px;' + (isOwn ? 'left:-30px' : 'right:-30px') + ';display:none;';
            var icon = document.createElement('i');
            icon.className = 'ti ti-arrow-back-up';
            replyBtn.appendChild(icon);
            var capturedName = senderName || 'Sen';
            var capturedContent = content;
            var capturedId = messageId;
            replyBtn.addEventListener('click', function () {
                setReply(capturedId, capturedName, capturedContent);
            });
            bubble.appendChild(replyBtn);
            bubble.addEventListener('mouseenter', function () { replyBtn.style.display = ''; });
            bubble.addEventListener('mouseleave', function () { replyBtn.style.display = 'none'; });
        }

        wrapper.appendChild(bubble);
        area.appendChild(wrapper);
        area.scrollTop = area.scrollHeight;
    }
})();
