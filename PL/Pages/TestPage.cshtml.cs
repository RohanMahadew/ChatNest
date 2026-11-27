using BLL.Models;
using BLL.Models.DTOs;
using BLL.Models.Enums;
using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace PL.Pages
{
    [Authorize]
    public class TestModel : PageModel
    {
        private readonly IChannelServiceInterface _channelService;

        public TestModel(IChannelServiceInterface channelService)
        {
            _channelService = channelService;
        }

        public List<ChannelDTO> DirectMessages { get; private set; } = [];
        public ChannelDTO? SelectedDirectMessage { get; private set; }
        public List<GroupChannelDTO> PrivateGroups { get; private set; } = [];
        public GroupChannelDTO? SelectedGroup { get; private set; }
        public List<ChannelMemberDTO> ChannelMembers { get; private set; } = [];
        public int CurrentUserId { get; private set; }

        public async Task<IActionResult> OnGetAsync(string? uuid)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }
            CurrentUserId = currentUserId;

            DirectMessages = await _channelService.GetDirectChatsForUserAsync(currentUserId);
            PrivateGroups = await _channelService.GetPrivateGroupsForUserAsync(currentUserId);
            if (!string.IsNullOrWhiteSpace(uuid))
            {
                if (!Guid.TryParse(uuid, out var channelUuid))
                {
                    return RedirectToPage("/TestPage", new { uuid = (string?)null });
                }

                SelectedGroup = await _channelService.GetPrivateGroupForUserAsync(channelUuid.ToString("D"), currentUserId);
                if (SelectedGroup is not null)
                {
                    if (SelectedGroup.Role != nameof(ChannelRole.Invited))
                    {
                        ChannelMembers = await _channelService.GetChannelMembersAsync(SelectedGroup.Uuid, currentUserId);
                    }
                }
                else
                {
                    SelectedDirectMessage = await _channelService.GetDirectChatForUserAsync(channelUuid.ToString("D"), currentUserId);
                    if (SelectedDirectMessage is null)
                    {
                        return RedirectToPage("/TestPage", new { uuid = (string?)null });
                    }

                    ChannelMembers = await _channelService.GetChannelMembersAsync(SelectedDirectMessage.Uuid, currentUserId);
                }
            }

            ViewData["DirectMessages"] = DirectMessages;
            ViewData["SelectedDirectMessage"] = SelectedDirectMessage;
            ViewData["PrivateGroups"] = PrivateGroups;
            ViewData["SelectedGroup"] = SelectedGroup;
            ViewData["ChannelMembers"] = ChannelMembers;
            ViewData["CurrentUserId"] = CurrentUserId;
            ViewData["ActiveTab"] = TempData["ActiveTab"] as string;
            return Page();
        }

        public async Task<IActionResult> OnPostStartDirectChatAsync(int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var channel = await _channelService.StartDirectChatAsync(currentUserId, targetUserId);
                TempData["ActiveTab"] = "dm";
                return RedirectToPage("/TestPage", new { uuid = channel.Uuid });
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastMessage"] = ex.Message;
                return RedirectToPage("/TestPage", new { uuid = (string?)null });
            }
        }

        public async Task<IActionResult> OnPostBlockDirectChatAsync(string uuid)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            if (!await _channelService.BlockDirectChatAsync(uuid, currentUserId))
            {
                return RedirectToPage("/TestPage", new { uuid = (string?)null });
            }

            TempData["ToastMessage"] = "User blocked.";
            return RedirectToPage("/TestPage", new { uuid = (string?)null });
        }

        public async Task<IActionResult> OnPostCreatePrivateGroupAsync(string groupName)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            try
            {
                var group = await _channelService.CreatePrivateGroupAsync(currentUserId, groupName);
                TempData["ActiveTab"] = "groups";
                return RedirectToPage("/TestPage", new { uuid = group.Uuid });
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ToastMessage"] = ex.Message;
                return RedirectToPage("/TestPage", new { uuid = (string?)null });
            }
        }

        public async Task<IActionResult> OnPostInviteToPrivateGroupAsync(string uuid, int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            if (!await _channelService.InviteUserToPrivateGroupAsync(uuid, currentUserId, targetUserId))
            {
                TempData["ToastMessage"] = "This user cannot be invited to the group.";
                return RedirectToPage("/TestPage", new { uuid });
            }

            TempData["ToastMessage"] = "Invitation sent.";
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostRespondToGroupInvitationAsync(string uuid, GroupInvitationAction action)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            if (!Enum.IsDefined(action))
            {
                return BadRequest();
            }

            if (!await _channelService.RespondToPrivateGroupInvitationAsync(uuid, currentUserId, action))
            {
                return RedirectToPage("/TestPage", new { uuid = (string?)null });
            }

            if (action == GroupInvitationAction.Join)
            {
                return RedirectToPage("/TestPage", new { uuid });
            }

            TempData["ToastMessage"] = action == GroupInvitationAction.Block
                ? "Group invitation blocked."
                : "Group invitation declined.";
            return RedirectToPage("/TestPage", new { uuid = (string?)null });
        }

        public async Task<IActionResult> OnPostPromoteGroupMemberAsync(string uuid, int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            TempData["ActiveTab"] = "groups";
            TempData["ToastMessage"] = await _channelService.PromoteGroupMemberAsync(uuid, currentUserId, targetUserId)
                ? "User promoted to Admin."
                : "Could not promote user.";
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostDemoteGroupMemberAsync(string uuid, int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            TempData["ActiveTab"] = "groups";
            TempData["ToastMessage"] = await _channelService.DemoteGroupMemberAsync(uuid, currentUserId, targetUserId)
                ? "Admin demoted to Member."
                : "Could not demote user.";
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostRemoveGroupMemberAsync(string uuid, int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            TempData["ActiveTab"] = "groups";
            TempData["ToastMessage"] = await _channelService.RemoveGroupMemberAsync(uuid, currentUserId, targetUserId)
                ? "User removed from group."
                : "Could not remove user.";
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostBlockGroupMemberAsync(string uuid, int targetUserId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            TempData["ActiveTab"] = "groups";
            TempData["ToastMessage"] = await _channelService.BlockGroupMemberAsync(uuid, currentUserId, targetUserId)
                ? "User blocked from group."
                : "Could not block user.";
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostToggleGroupVisibilityAsync(string uuid)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            var updatedType = await _channelService.ToggleGroupVisibilityAsync(uuid, currentUserId);
            TempData["ActiveTab"] = "groups";
            TempData["ToastMessage"] = updatedType switch
            {
                ChannelType.PUBLIC => "Group is now public.",
                ChannelType.PRIVATE => "Group is now private.",
                _ => "Could not change group visibility."
            };
            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostJoinPublicGroupAsync(string uuid)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            TempData["ActiveTab"] = "groups";
            if (!await _channelService.JoinPublicGroupAsync(uuid, currentUserId))
            {
                TempData["ToastMessage"] = "You cannot join this group.";
                return RedirectToPage("/TestPage", new { uuid = (string?)null });
            }

            return RedirectToPage("/TestPage", new { uuid });
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
