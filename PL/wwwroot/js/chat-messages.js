const messageList = document.querySelector('[data-message-list]');

if (messageList) {
    const channelUuid = messageList.dataset.channelUuid;
    const currentUserId = Number(messageList.dataset.currentUserId);
    const currentUserRole = messageList.dataset.currentUserRole || '';
    const isGroupChat = messageList.dataset.isGroup === 'true';

    function canDeleteMessage(isCurrentUser, senderRole) {
        if (isCurrentUser) {
            return true;
        }
        if (!isGroupChat) {
            return false;
        }
        if (currentUserRole === 'SuperAdmin') {
            return senderRole !== 'SuperAdmin';
        }
        if (currentUserRole === 'Admin') {
            return senderRole !== 'SuperAdmin' && senderRole !== 'Admin';
        }
        return false;
    }
    const messageForm = document.getElementById('messageForm');
    const messageInput = document.getElementById('messageInput');
    const sendButton = document.getElementById('sendMessageButton');
    const sendStatus = document.getElementById('messageSendStatus');
    const messageTemplate = document.getElementById('chatMessageTemplate');
    const renderedMessageIds = new Set();
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/chatHub')
        .withAutomaticReconnect()
        .build();

    function setSendStatus(message) {
        sendStatus.textContent = message;
        sendStatus.classList.toggle('hidden', !message);
    }

    function ensureEmptyMessagesState() {
        const hasMessages = messageList.querySelector('[data-message-row]') !== null;
        const existingEmpty = messageList.querySelector('[data-empty-messages]');
        if (!hasMessages && !existingEmpty) {
            const empty = document.createElement('p');
            empty.dataset.emptyMessages = '';
            empty.className = 'py-12 text-center text-xs text-white/40';
            empty.textContent = 'No messages yet.';
            messageList.appendChild(empty);
        } else if (hasMessages && existingEmpty) {
            existingEmpty.remove();
        }
    }

    function getMessageRow(messageId) {
        return messageList.querySelector(`[data-message-row][data-message-id="${messageId}"]`);
    }

    function setEditMode(row, editing) {
        const text = row.querySelector('[data-message-text]');
        const editBox = row.querySelector('[data-message-edit-box]');
        const editInput = row.querySelector('[data-message-edit-input]');
        const actions = row.querySelector('[data-message-actions]');

        text.classList.toggle('hidden', editing);
        editBox.classList.toggle('hidden', !editing);

        if (row.dataset.isCurrentUser === 'true') {
            actions.classList.toggle('hidden', editing);
            actions.classList.toggle('flex', !editing);
        }

        if (editing) {
            editInput.value = row.dataset.messageContent ?? '';
            editInput.focus();
            editInput.setSelectionRange(editInput.value.length, editInput.value.length);
        }
    }

    function setDeleteConfirmMode(row, confirming) {
        const actions = row.querySelector('[data-message-actions]');
        const defaultActions = row.querySelector('[data-actions-default]');
        const confirmActions = row.querySelector('[data-actions-confirm]');

        defaultActions.classList.toggle('hidden', confirming);
        defaultActions.classList.toggle('flex', !confirming);
        confirmActions.classList.toggle('hidden', !confirming);
        confirmActions.classList.toggle('flex', confirming);
        actions.classList.toggle('opacity-100', confirming);
        actions.classList.toggle('opacity-0', !confirming);
        actions.classList.toggle('pointer-events-auto', confirming);
        actions.classList.toggle('pointer-events-none', !confirming);
    }

    function applyMessageContent(row, message) {
        const text = row.querySelector('[data-message-text]');
        const edited = row.querySelector('[data-message-edited]');
        const content = message.content ?? '';
        const isEdited = Boolean(
            message.isEdited
            || (message.updatedAt && message.createdAt
                && new Date(message.updatedAt).getTime() > new Date(message.createdAt).getTime())
        );

        row.dataset.messageContent = content;
        text.textContent = content;
        edited.classList.toggle('hidden', !isEdited);
    }

    function renderMessage(message) {
        if (!message) {
            return;
        }

        if (renderedMessageIds.has(message.id)) {
            updateRenderedMessage(message);
            return;
        }

        renderedMessageIds.add(message.id);
        messageList.querySelector('[data-empty-messages]')?.remove();

        const fragment = messageTemplate.content.cloneNode(true);
        const row = fragment.querySelector('[data-message-row]');
        const layout = fragment.querySelector('[data-message-layout]');
        const avatar = fragment.querySelector('[data-message-avatar]');
        const sender = fragment.querySelector('[data-message-sender]');
        const bubble = fragment.querySelector('[data-message-bubble]');
        const actions = fragment.querySelector('[data-message-actions]');
        const editButton = fragment.querySelector('[data-action="edit-message"]');
        const time = fragment.querySelector('[data-message-time]');
        const isCurrentUser = Number(message.userId) === currentUserId;
        const senderRole = message.senderRole || '';
        const canDelete = canDeleteMessage(isCurrentUser, senderRole);
        const senderName = message.senderName || `User ${message.userId}`;

        row.dataset.messageId = String(message.id);
        row.dataset.isCurrentUser = String(isCurrentUser);
        row.dataset.canDelete = String(canDelete);
        row.classList.toggle('items-end', isCurrentUser);
        row.classList.toggle('items-start', !isCurrentUser);
        layout.classList.toggle('flex-row-reverse', isCurrentUser);

        if (isCurrentUser) {
            avatar.classList.add('hidden');
            avatar.classList.remove('flex');
            sender.classList.add('hidden');
            sender.classList.remove('block');
        } else {
            avatar.textContent = senderName.slice(0, 2).toUpperCase();
            avatar.title = senderName;
            if (isGroupChat) {
                sender.textContent = senderName;
                sender.classList.remove('hidden');
                sender.classList.add('block');
            } else {
                sender.textContent = '';
                sender.classList.add('hidden');
                sender.classList.remove('block');
            }
        }

        if (isCurrentUser || canDelete) {
            actions.classList.remove('hidden');
            actions.classList.add('flex');
            if (editButton) {
                editButton.classList.toggle('hidden', !isCurrentUser);
            }
        }

        bubble.classList.toggle('bg-[#603273]/60', isCurrentUser);
        applyMessageContent(row, message);
        time.textContent = message.createdAt ? new Date(message.createdAt).toLocaleString() : '';
        messageList.appendChild(fragment);
        messageList.scrollTop = messageList.scrollHeight;
    }

    function updateRenderedMessage(message) {
        if (!message) {
            return;
        }

        const row = getMessageRow(message.id);
        if (!row) {
            return;
        }

        applyMessageContent(row, message);
        setEditMode(row, false);
    }

    function deleteRenderedMessage(messageId) {
        const numericId = Number(messageId);
        renderedMessageIds.delete(numericId);
        getMessageRow(numericId)?.remove();
        ensureEmptyMessagesState();
    }

    async function saveMessageEdit(row) {
        const messageId = Number(row.dataset.messageId);
        const editInput = row.querySelector('[data-message-edit-input]');
        const saveButton = row.querySelector('[data-action="save-edit"]');
        const cancelButton = row.querySelector('[data-action="cancel-edit"]');
        const newContent = editInput.value;

        if (!newContent.trim()) {
            setSendStatus('Message cannot be empty.');
            editInput.focus();
            return;
        }

        if (newContent.trim() === (row.dataset.messageContent ?? '')) {
            setEditMode(row, false);
            setSendStatus('');
            return;
        }

        if (connection.state !== signalR.HubConnectionState.Connected) {
            setSendStatus('Chat is not connected yet.');
            return;
        }

        saveButton.disabled = true;
        cancelButton.disabled = true;
        try {
            await connection.invoke('UpdateMessage', channelUuid, messageId, newContent);
            setSendStatus('');
        } catch {
            setSendStatus('Message could not be updated. Please try again.');
        } finally {
            saveButton.disabled = false;
            cancelButton.disabled = false;
        }
    }

    async function confirmMessageDelete(row) {
        const messageId = Number(row.dataset.messageId);
        const confirmButton = row.querySelector('[data-action="confirm-delete"]');
        const cancelButton = row.querySelector('[data-action="cancel-delete"]');

        if (connection.state !== signalR.HubConnectionState.Connected) {
            setSendStatus('Chat is not connected yet.');
            return;
        }

        confirmButton.disabled = true;
        cancelButton.disabled = true;
        try {
            await connection.invoke('DeleteMessage', channelUuid, messageId);
            setSendStatus('');
        } catch {
            setSendStatus('Message could not be deleted. Please try again.');
            confirmButton.disabled = false;
            cancelButton.disabled = false;
            setDeleteConfirmMode(row, false);
        }
    }

    async function joinAndLoadMessages() {
        await connection.invoke('JoinChannel', channelUuid);
        const messages = await connection.invoke('GetMessages', channelUuid);
        const serverIds = new Set(messages.map(m => m.id));
        Array.from(renderedMessageIds)
            .filter(id => !serverIds.has(id))
            .forEach(deleteRenderedMessage);
        messages.forEach(renderMessage);
        ensureEmptyMessagesState();
    }

    connection.on('ReceiveMessage', renderMessage);
    connection.on('MessageUpdated', updateRenderedMessage);
    connection.on('MessageDeleted', deleteRenderedMessage);

    messageList.addEventListener('click', event => {
        const actionElement = event.target.closest('[data-action]');
        if (!actionElement) {
            return;
        }

        const row = actionElement.closest('[data-message-row]');
        if (!row) {
            return;
        }

        const isCurrentUser = row.dataset.isCurrentUser === 'true';
        const canDelete = isCurrentUser || row.dataset.canDelete === 'true';

        switch (actionElement.dataset.action) {
            case 'edit-message':
                if (!isCurrentUser) return;
                setDeleteConfirmMode(row, false);
                setEditMode(row, true);
                break;
            case 'cancel-edit':
                if (!isCurrentUser) return;
                setEditMode(row, false);
                setSendStatus('');
                break;
            case 'save-edit':
                if (!isCurrentUser) return;
                saveMessageEdit(row);
                break;
            case 'delete-message':
                if (!canDelete) return;
                setDeleteConfirmMode(row, true);
                break;
            case 'cancel-delete':
                if (!canDelete) return;
                setDeleteConfirmMode(row, false);
                break;
            case 'confirm-delete':
                if (!canDelete) return;
                confirmMessageDelete(row);
                break;
        }
    });

    messageList.addEventListener('keydown', event => {
        const editInput = event.target.closest('[data-message-edit-input]');
        if (!editInput) {
            return;
        }

        const row = editInput.closest('[data-message-row]');
        if (!row) {
            return;
        }

        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            saveMessageEdit(row);
        } else if (event.key === 'Escape') {
            event.preventDefault();
            setEditMode(row, false);
            setSendStatus('');
        }
    });
    connection.onreconnecting(() => {
        sendButton.disabled = true;
        setSendStatus('Reconnecting to chat...');
    });
    connection.onreconnected(async () => {
        try {
            await joinAndLoadMessages();
            sendButton.disabled = false;
            setSendStatus('');
        } catch {
            setSendStatus('Unable to rejoin this channel.');
        }
    });

    sendButton.disabled = true;
    connection.start()
        .then(async () => {
            await joinAndLoadMessages();
            sendButton.disabled = false;
        })
        .catch(() => setSendStatus('Unable to connect to chat.'));

    messageForm.addEventListener('submit', async event => {
        event.preventDefault();
        const content = messageInput.value;
        if (!content.trim()) {
            return;
        }

        if (connection.state !== signalR.HubConnectionState.Connected) {
            setSendStatus('Chat is not connected yet.');
            return;
        }

        sendButton.disabled = true;
        try {
            await connection.invoke('SendMessage', channelUuid, content);
            messageInput.value = '';
            setSendStatus('');
        } catch {
            setSendStatus('Message could not be sent. Please try again.');
        } finally {
            sendButton.disabled = false;
            messageInput.focus();
        }
    });

    messageInput.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            messageForm.requestSubmit();
        }
    });
}
