using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CodePilot.Areas.Identity.Data;
using CodePilot.Data;
using CodePilot.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CodePilot.Controllers
{
    public class ChatController : Controller
    {
        UserManager<ApplicationUser> _userManager;
        private readonly CodePilotContext _dbContext;
        public ChatController(CodePilotContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [Authorize]
        public IActionResult Chat()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ChatHistory()
        {
            var userId = (await _userManager.FindByNameAsync(User.Identity.Name)).Id;
            var chats = await _dbContext.Chats
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastModified)
                .ToListAsync();
            return View(chats);
        }

        [Authorize]
        public async Task<IActionResult> ChatMessages(int chatId)
        {
            var messages = await _dbContext.Messages
                .Where(c => c.ChatId == chatId)
                .ToListAsync();
            return View(messages);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
        {
            Response.ContentType = "text/event-stream"; // "text/plain"

            using var client = new HttpClient();
            var apiUrl = "https://router.huggingface.co/nebius/v1/chat/completions";
            var token = "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";
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            //---

            var chatHistory = await _dbContext.Messages
                .Where(m => m.ChatId == request.ChatId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var messages = chatHistory.Select(m => new
            {
                role = m.IsUserMessage ? "user" : "assistant",
                content = m.Content
            }).ToList();

            messages.Add(new { role = "user", content = request.Message });

            //---
            var payload = new
            {
                model = "google/gemma-3-27b-it-fast",
                messages = messages, // new[] { new { role = "user", content = request.Message } },
                max_tokens = 1024,
                temperature = 0.5,
                stream = true
            };
            //---

            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = httpContent
            };

            using var response = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None);
            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(responseStream);
            string? line;
            var botResponse = new StringBuilder();
            while ((line = await reader.ReadLineAsync()) != null)
            {
                try
                {
                    if (line.StartsWith("data: "))
                    {
                        line = line.Substring(6);
                    }
                    using var jsonDoc = JsonDocument.Parse(line);
                    if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var delta = choices[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var contentElement))
                        {
                            var content = contentElement.GetString();
                            if (!string.IsNullOrEmpty(content))
                            {
                                await Response.WriteAsync(content);
                                await Response.Body.FlushAsync();
                                //botResponse.AppendLine(content);
                                botResponse.Append(content);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore non-JSON lines
                }
            }

            //------------------------------------------------------------------

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Chat chat = null;
            if (request.ChatId.HasValue)
            {
                chat = await _dbContext.Chats.FindAsync(request.ChatId.Value);
                if (chat != null)
                {
                    chat.LastModified = DateTime.UtcNow;
                    _dbContext.Entry(chat).State = EntityState.Modified;
                }
                _dbContext.Chats.Update(chat);
            }
            if (chat == null)
            {
                chat = new Chat
                {
                    UserId = userId,
                    Title = TrimText(request.Message, 20),
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                };
                _dbContext.Chats.Add(chat);
                await _dbContext.SaveChangesAsync();
            }
            var userMessage = new Message
            {
                ChatId = chat.Id,
                IsUserMessage = true,
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            };

            var botMessage = new Message
            {
                ChatId = chat.Id,
                IsUserMessage = false,
                Content = botResponse.ToString(),
                Timestamp = DateTime.UtcNow
            };
            _dbContext.Messages.Add(userMessage);
            _dbContext.Messages.Add(botMessage);
            await _dbContext.SaveChangesAsync();

            return new EmptyResult();
        }

        static string TrimText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            text = char.ToUpper(text[0]) + text.Substring(1);
            if (text.Length <= maxLength) return text;
            return text.Length > maxLength ? text.Substring(0, maxLength).Trim() + "..." : text;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
            // public List<Message> ConversationHistory { get; set; }

            public int? ChatId { get; set; }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteChat([FromBody] int chatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
            if (chat == null)
            {
                return NotFound();
            }
            //_dbContext.Messages.RemoveRange(chat.Messages);
            _dbContext.Chats.Remove(chat);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
