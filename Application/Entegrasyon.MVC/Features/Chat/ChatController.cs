using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Chat;
using Entegrasyon.Entity.Dtos.Chat;
using Entegrasyon.MVC.Infrastructure.Extensions;

namespace Entegrasyon.MVC.Features.Chat;

[Authorize]
public class ChatController(IChatManager chatManager) : Controller
{
    [HttpGet("/chat")]
    public async Task<IActionResult> Index(long? conversationId = null)
    {
        ViewData.SetPageTitle("Mesajlar");
        ViewData.SetActiveNav("chat");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return RedirectToAction("Login", "Auth");

        var uid = Guid.Parse(userId);
        var conversations = await chatManager.GetConversationsForUser(uid);
        ViewBag.ActiveConversationId = conversationId;
        ViewBag.CurrentUserId = uid;

        if (conversationId.HasValue)
        {
            var messages = await chatManager.GetMessages(conversationId.Value, uid);
            ViewBag.Messages = messages;

            // Mark conversation as read when opened
            await chatManager.MarkConversationAsRead(conversationId.Value, uid);
        }

        return View(conversations);
    }

    [HttpGet("/chat/{conversationId:long}")]
    public async Task<IActionResult> Conversation(long conversationId)
    {
        ViewData.SetPageTitle("Mesajlar");
        ViewData.SetActiveNav("chat");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return RedirectToAction("Login", "Auth");

        var uid = Guid.Parse(userId);
        var conversations = await chatManager.GetConversationsForUser(uid);
        var messages = await chatManager.GetMessages(conversationId, uid);

        await chatManager.MarkConversationAsRead(conversationId, uid);

        ViewBag.ActiveConversationId = conversationId;
        ViewBag.CurrentUserId = uid;
        ViewBag.Messages = messages;

        return View("~/Features/Chat/Views/Index.cshtml", conversations);
    }

    [HttpPost("/chat/send")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var dto = new SendChatMessageDto(request.ConversationId, request.Content);
        await chatManager.SendMessage(Guid.Parse(userId), dto);

        return Ok();
    }

    [HttpPost("/chat/new")]
    public async Task<IActionResult> NewConversation([FromBody] NewConversationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var uid = Guid.Parse(userId);
        var conversation = await chatManager.GetOrCreateDirectConversation(uid, request.TargetUserId);

        return Ok(new { conversationId = conversation.Id });
    }

    [HttpGet("/chat/users")]
    public async Task<IActionResult> AvailableUsers()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var users = await chatManager.GetAvailableChatUsers(Guid.Parse(userId));
        var result = users.Select(u => new { u.Id, name = $"{u.Name} {u.Surname}" });

        return Json(result);
    }

    [HttpGet("/chat/{conversationId:long}/messages")]
    public async Task<IActionResult> Messages(long conversationId, long? before = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var messages = await chatManager.GetMessages(conversationId, Guid.Parse(userId), beforeMessageId: before);
        return Json(messages);
    }

    public sealed record SendMessageRequest(long ConversationId, string Content);
    public sealed record NewConversationRequest(Guid TargetUserId);
}
