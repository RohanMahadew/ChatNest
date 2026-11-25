using BLL.Models;
using BLL.Models.DTOs;
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

        public async Task<IActionResult> OnGetAsync(string? uuid)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            DirectMessages = await _channelService.GetDirectChatsForUserAsync(currentUserId);
            PrivateGroups = await _channelService.GetPrivateGroupsForUserAsync(currentUserId);
            if (!string.IsNullOrWhiteSpace(uuid))
            {
                if (!Guid.TryParse(uuid, out var channelUuid))
                {
                    return NotFound();
                }

                SelectedGroup = await _channelService.GetPrivateGroupForUserAsync(channelUuid.ToString("D"), currentUserId);
                if (SelectedGroup is null)
                {
                    SelectedDirectMessage = await _channelService.GetDirectChatForUserAsync(channelUuid.ToString("D"), currentUserId);
                    if (SelectedDirectMessage is null)
                    {
                        return NotFound();
                    }
                }
            }

            ViewData["DirectMessages"] = DirectMessages;
            ViewData["SelectedDirectMessage"] = SelectedDirectMessage;
            ViewData["PrivateGroups"] = PrivateGroups;
            ViewData["SelectedGroup"] = SelectedGroup;
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
                return RedirectToPage("/TestPage", new { uuid = channel.Uuid });
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
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
                return RedirectToPage("/TestPage", new { uuid = group.Uuid });
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
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
                return BadRequest();
            }

            return RedirectToPage("/TestPage", new { uuid });
        }

        public async Task<IActionResult> OnPostRespondToGroupInvitationAsync(string uuid, bool accept)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
            {
                return Challenge();
            }

            if (!await _channelService.RespondToPrivateGroupInvitationAsync(uuid, currentUserId, accept))
            {
                return NotFound();
            }

            return accept
                ? RedirectToPage("/TestPage", new { uuid })
                : RedirectToPage("/TestPage");
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
