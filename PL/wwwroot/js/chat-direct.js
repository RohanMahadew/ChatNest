let userSearchTimeout;
let userSearchController;
let userSearchMode = 'direct';

function showUserSearchStatus(message) {
    const status = document.getElementById('userSearchStatus');
    status.textContent = message;
    status.classList.remove('hidden');
}

function getDefaultSearchStatusMessage() {
    return userSearchMode === 'invite'
        ? 'Enter a username or email to find users.'
        : 'Enter a username or email to find users or public groups.';
}

function setSearchFormHandler(handlerName) {
    const form = document.getElementById('startDirectChatForm');
    const actionUrl = new URL(form.action, window.location.href);
    actionUrl.searchParams.set('handler', handlerName);
    form.action = actionUrl.toString();
}

function resetUserSearch() {
    clearTimeout(userSearchTimeout);
    userSearchController?.abort();
    document.getElementById('newUsername').value = '';
    document.getElementById('userSearchResults').replaceChildren();
    showUserSearchStatus(getDefaultSearchStatusMessage());
}

function openAddContactModal(mode = 'direct', groupUuid = '') {
    const modal = document.getElementById('addFriendModal');
    const createGroupContainer = document.getElementById('createGroupActionContainer');
    userSearchMode = mode;
    setSearchFormHandler(mode === 'invite' ? 'InviteToPrivateGroup' : 'StartDirectChat');
    document.getElementById('inviteGroupUuid').value = groupUuid;
    document.getElementById('addContactHeading').textContent = mode === 'invite' ? 'Invite users to the group' : 'Start a New Conversation';
    document.getElementById('userSearchDescription').textContent = mode === 'invite'
        ? 'Search for people to invite to this private group.'
        : 'Search for users or public groups, or create a new group.';
    if (createGroupContainer) {
        createGroupContainer.classList.toggle('hidden', mode === 'invite');
    }
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

function renderUserSearchResults(users, publicGroups = []) {
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

    publicGroups.forEach(group => {
        const item = document.createElement('li');
        const button = document.createElement('button');
        button.type = 'submit';
        button.dataset.publicGroupUuid = group.uuid;
        button.className = 'flex w-full items-center justify-between rounded-lg border border-[#a875bc]/20 bg-[#603273]/15 px-3 py-2 text-left text-sm text-white transition hover:bg-[#603273]/30';
        const label = document.createElement('span');
        label.className = 'flex items-center gap-2';
        const name = document.createElement('span');
        name.textContent = group.name;
        const badge = document.createElement('span');
        badge.className = 'rounded-full border border-[#a875bc]/40 bg-[#603273]/40 px-2 py-0.5 text-[10px] text-[#c79bd5]';
        badge.textContent = 'Public group';
        label.append(name, badge);
        const action = document.createElement('span');
        action.className = 'text-xs font-medium text-[#c79bd5]';
        action.textContent = group.isMember ? 'Open group' : 'Join group';
        button.append(label, action);
        item.appendChild(button);
        results.appendChild(item);
    });

    if (users.length === 0 && publicGroups.length === 0) {
        showUserSearchStatus(userSearchMode === 'invite' ? 'No users found.' : 'No users or public groups found.');
    }
}

function handleUserSearch(value) {
    clearTimeout(userSearchTimeout);
    userSearchController?.abort();

    const query = value.trim();
    document.getElementById('userSearchResults').replaceChildren();
    if (!query) {
        showUserSearchStatus(getDefaultSearchStatusMessage());
        return;
    }

    showUserSearchStatus(userSearchMode === 'invite' ? 'Searching users...' : 'Searching users and groups...');
    userSearchTimeout = setTimeout(async () => {
        userSearchController = new AbortController();
        try {
            const params = new URLSearchParams({ query });
            const requests = [
                fetch(`/api/users/search?${params}`, { signal: userSearchController.signal })
            ];
            if (userSearchMode === 'direct') {
                requests.push(fetch(`/api/groups/search?${params}`, { signal: userSearchController.signal }));
            }

            const responses = await Promise.all(requests);
            if (responses.some(response => response.status === 401)) {
                showUserSearchStatus('Sign in to search.');
                return;
            }
            if (responses.some(response => !response.ok)) {
                throw new Error('Search failed.');
            }

            const users = await responses[0].json();
            const publicGroups = responses[1] ? await responses[1].json() : [];
            renderUserSearchResults(users, publicGroups);
        } catch (error) {
            if (error.name !== 'AbortError') {
                showUserSearchStatus('Unable to search right now.');
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

const ACTIVE_TAB_STORAGE_KEY = 'chatnest.activeTab';
const VALID_TABS = new Set(['dm', 'groups', 'notifications']);

function selectConversationList(filter, persist = true) {
    if (!VALID_TABS.has(filter)) {
        return;
    }

    if (persist) {
        sessionStorage.setItem(ACTIVE_TAB_STORAGE_KEY, filter);
    }

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

function restoreActiveConversationTab() {
    const sidebar = document.getElementById('sidebarSection');
    const explicitTab = sidebar?.dataset.explicitTab;
    if (explicitTab && VALID_TABS.has(explicitTab)) {
        selectConversationList(explicitTab);
        return;
    }

    const savedTab = sessionStorage.getItem(ACTIVE_TAB_STORAGE_KEY);
    if (savedTab && VALID_TABS.has(savedTab)) {
        selectConversationList(savedTab, false);
    }
}

restoreActiveConversationTab();

document.getElementById('newUsername').addEventListener('input', event => handleUserSearch(event.target.value));
document.getElementById('contactSearchInput').addEventListener('input', event => filterConversations(event.target.value));
document.getElementById('clearSearchBtn').addEventListener('click', () => {
    const search = document.getElementById('contactSearchInput');
    search.value = '';
    filterConversations('');
});
document.getElementById('startDirectChatForm').addEventListener('click', event => {
    const userButton = event.target.closest('[data-user-id]');
    if (userButton) {
        setSearchFormHandler(userSearchMode === 'invite' ? 'InviteToPrivateGroup' : 'StartDirectChat');
        document.getElementById('targetUserId').value = userButton.dataset.userId;
        return;
    }

    const groupButton = event.target.closest('[data-public-group-uuid]');
    if (groupButton) {
        setSearchFormHandler('JoinPublicGroup');
        document.getElementById('inviteGroupUuid').value = groupButton.dataset.publicGroupUuid;
    }
});
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

function setChannelMenuOpen(open) {
    const menu = document.getElementById('channelContextMenu');
    const toggle = document.querySelector('[data-action="toggle-channel-menu"]');
    if (!menu || !toggle) {
        return;
    }

    menu.classList.toggle('hidden', !open);
    toggle.setAttribute('aria-expanded', String(open));
}

document.addEventListener('click', event => {
    const actionEl = event.target.closest('[data-action]');
    const action = actionEl?.dataset.action;
    if (action === 'open-add-contact') openAddContactModal();
    if (action === 'close-add-contact') closeAddContactModal();
    if (action === 'invite-to-group') openAddContactModal('invite', actionEl.dataset.groupUuid);
    if (action === 'open-create-group') {
        closeAddContactModal();
        const modal = document.getElementById('createGroupModal');
        modal.classList.remove('hidden');
        modal.classList.add('flex');
        modal.setAttribute('aria-hidden', 'false');
        document.getElementById('groupName').focus();
    }
    if (action === 'close-create-group') closeCreateGroupModal();
    if (action === 'show-contacts') document.getElementById('sidebarSection').classList.remove('hidden');
    if (action === 'toggle-channel-menu') {
        const menu = document.getElementById('channelContextMenu');
        setChannelMenuOpen(menu ? menu.classList.contains('hidden') : false);
    } else if (!event.target.closest('#channelContextMenu')) {
        setChannelMenuOpen(false);
    }
    const filter = event.target.closest('[data-filter]')?.dataset.filter;
    if (filter) selectConversationList(filter);
});
document.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
        closeAddContactModal();
        closeCreateGroupModal();
        setChannelMenuOpen(false);
    }
});

const toastNotification = document.getElementById('toastNotification');
if (toastNotification?.dataset.toastMessage) {
    toastNotification.classList.remove('translate-y-16', 'opacity-0');
    setTimeout(() => {
        toastNotification.classList.add('translate-y-16', 'opacity-0');
    }, 4000);
}
