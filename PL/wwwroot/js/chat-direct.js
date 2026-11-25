let userSearchTimeout;
let userSearchController;
let userSearchMode = 'direct';

function showUserSearchStatus(message) {
    const status = document.getElementById('userSearchStatus');
    status.textContent = message;
    status.classList.remove('hidden');
}

function resetUserSearch() {
    clearTimeout(userSearchTimeout);
    userSearchController?.abort();
    document.getElementById('newUsername').value = '';
    document.getElementById('userSearchResults').replaceChildren();
    showUserSearchStatus('Enter a username or email to find users.');
}

function openAddContactModal(mode = 'direct', groupUuid = '') {
    const modal = document.getElementById('addFriendModal');
    const form = document.getElementById('startDirectChatForm');
    const actionUrl = new URL(form.action, window.location.href);
    userSearchMode = mode;
    actionUrl.searchParams.set('handler', mode === 'invite' ? 'InviteToPrivateGroup' : 'StartDirectChat');
    form.action = actionUrl.toString();
    document.getElementById('inviteGroupUuid').value = groupUuid;
    document.getElementById('addContactHeading').textContent = mode === 'invite' ? 'Invite users to the group' : 'Start a New Conversation';
    document.getElementById('userSearchDescription').textContent = mode === 'invite'
        ? 'Search for people to invite to this private group.'
        : 'Search for users by their ChatNest handle or email address.';
    modal.classList.remove('hidden');
    modal.classList.add('flex');
    modal.setAttribute('aria-hidden', 'false');
    resetUserSearch();
    document.getElementById('newUsername').focus();
}

function closeAddContactModal() {
    const modal = document.getElementById('addFriendModal');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
    modal.setAttribute('aria-hidden', 'true');
    clearTimeout(userSearchTimeout);
    userSearchController?.abort();
}

function renderUserSearchResults(users) {
    const results = document.getElementById('userSearchResults');
    results.replaceChildren();
    document.getElementById('userSearchStatus').classList.add('hidden');

    users.forEach(user => {
        const item = document.createElement('li');
        const button = document.createElement('button');
        button.type = 'submit';
        button.dataset.userId = user.id;
        button.className = 'flex w-full items-center justify-between rounded-lg border border-white/5 bg-white/[0.03] px-3 py-2 text-left text-sm text-white transition hover:bg-white/[0.08]';
        const username = document.createElement('span');
        username.textContent = `@${user.userName ?? user.username}`;
        const action = document.createElement('span');
        action.className = 'text-xs font-medium text-[#b98acb]';
        action.textContent = userSearchMode === 'invite' ? 'Invite' : 'Open chat';
        button.append(username, action);
        item.appendChild(button);
        results.appendChild(item);
    });

    if (users.length === 0) {
        showUserSearchStatus('No users found.');
    }
}

function handleUserSearch(value) {
    clearTimeout(userSearchTimeout);
    userSearchController?.abort();

    const query = value.trim();
    document.getElementById('userSearchResults').replaceChildren();
    if (!query) {
        showUserSearchStatus('Enter a username or email to find users.');
        return;
    }

    showUserSearchStatus('Searching users...');
    userSearchTimeout = setTimeout(async () => {
        userSearchController = new AbortController();
        try {
            const params = new URLSearchParams({ query });
            const response = await fetch(`/api/users/search?${params}`, { signal: userSearchController.signal });
            if (response.status === 401) {
                showUserSearchStatus('Sign in to search for users.');
                return;
            }
            if (!response.ok) {
                throw new Error('User search failed.');
            }

            renderUserSearchResults(await response.json());
        } catch (error) {
            if (error.name !== 'AbortError') {
                showUserSearchStatus('Unable to search for users right now.');
            }
        }
    }, 250);
}

function filterConversations(value) {
    const query = value.trim().toLowerCase();
    document.querySelectorAll('[data-direct-chat]').forEach(chat => {
        chat.classList.toggle('hidden', !chat.dataset.username.toLowerCase().includes(query));
    });
    document.querySelectorAll('[data-group-chat]').forEach(group => {
        group.classList.toggle('hidden', !group.dataset.groupName.toLowerCase().includes(query));
    });
}

function selectConversationList(filter) {
    document.querySelectorAll('[data-conversation-list]').forEach(list => {
        list.classList.toggle('hidden', list.dataset.conversationList !== filter);
    });
    document.querySelectorAll('[data-filter]').forEach(tab => {
        const selected = tab.dataset.filter === filter;
        tab.setAttribute('aria-selected', selected.toString());
        tab.classList.toggle('bg-[#603273]', selected);
        tab.classList.toggle('text-white', selected);
        tab.classList.toggle('shadow-sm', selected);
        tab.classList.toggle('font-semibold', selected);
        tab.classList.toggle('text-white/60', !selected);
        tab.classList.toggle('font-medium', !selected);
    });
}

document.getElementById('newUsername').addEventListener('input', event => handleUserSearch(event.target.value));
document.getElementById('contactSearchInput').addEventListener('input', event => filterConversations(event.target.value));
document.getElementById('clearSearchBtn').addEventListener('click', () => {
    const search = document.getElementById('contactSearchInput');
    search.value = '';
    filterConversations('');
});
document.getElementById('startDirectChatForm').addEventListener('click', event => {
    const button = event.target.closest('[data-user-id]');
    if (button) {
        document.getElementById('targetUserId').value = button.dataset.userId;
    }
});
document.getElementById('messageForm').addEventListener('submit', event => event.preventDefault());
document.getElementById('addFriendModal').addEventListener('click', event => {
    if (event.target === event.currentTarget) closeAddContactModal();
});
document.getElementById('createGroupModal').addEventListener('click', event => {
    if (event.target === event.currentTarget) closeCreateGroupModal();
});

function closeCreateGroupModal() {
    const modal = document.getElementById('createGroupModal');
    modal.classList.add('hidden');
    modal.classList.remove('flex');
    modal.setAttribute('aria-hidden', 'true');
}

document.addEventListener('click', event => {
    const action = event.target.closest('[data-action]')?.dataset.action;
    if (action === 'open-add-contact') openAddContactModal();
    if (action === 'close-add-contact') closeAddContactModal();
    if (action === 'invite-to-group') openAddContactModal('invite', event.target.closest('[data-action]').dataset.groupUuid);
    if (action === 'open-create-group') {
        const modal = document.getElementById('createGroupModal');
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        modal.setAttribute('aria-hidden', 'false');
        document.getElementById('groupName').focus();
    }
    if (action === 'close-create-group') closeCreateGroupModal();
    if (action === 'show-contacts') document.getElementById('sidebarSection').classList.remove('hidden');
    const filter = event.target.closest('[data-filter]')?.dataset.filter;
    if (filter) selectConversationList(filter);
});
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
        closeAddContactModal();
        closeCreateGroupModal();
    }
});
