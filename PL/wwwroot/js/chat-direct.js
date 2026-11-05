let userSearchTimeout;
let userSearchController;

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

function openAddContactModal() {
    const modal = document.getElementById('addFriendModal');
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
        action.textContent = 'Open chat';
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

function filterDirectMessages(value) {
    const query = value.trim().toLowerCase();
    document.querySelectorAll('[data-direct-chat]').forEach(chat => {
        chat.classList.toggle('hidden', !chat.dataset.username.toLowerCase().includes(query));
    });
}

document.getElementById('newUsername').addEventListener('input', event => handleUserSearch(event.target.value));
document.getElementById('contactSearchInput').addEventListener('input', event => filterDirectMessages(event.target.value));
document.getElementById('clearSearchBtn').addEventListener('click', () => {
    const search = document.getElementById('contactSearchInput');
    search.value = '';
    filterDirectMessages('');
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
document.addEventListener('click', event => {
    const action = event.target.closest('[data-action]')?.dataset.action;
    if (action === 'open-add-contact') openAddContactModal();
    if (action === 'close-add-contact') closeAddContactModal();
    if (action === 'show-contacts') document.getElementById('sidebarSection').classList.remove('hidden');
});
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') closeAddContactModal();
});
