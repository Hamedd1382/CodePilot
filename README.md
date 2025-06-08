# مستندات پروژه CodePilot


### 1. مقدمه

پروژه CodePilot یک ربات گفتگو (Chatbot) مبتنی بر ASP.NET Core 6.0 است که با استفاده از مدل زبان Gemma 3.0 27B از طریق API هوش مصنوعی HuggingFace عمل می‌کند. این اپلیکیشن برای پاسخ به سوالات برنامه‌نویسی و تجزیه و تحلیل کد طراحی شده است و پاسخ‌ها را به صورت استریم (Stream) به کاربر ارسال می‌کند.

### 2. ساختار کلی پروژه

```
CodePilot/
├── Areas/
│   └── Identity/
│       ├── Data/
│       │   ├── ApplicationUser.cs
│       │   └── CodePilotContext.cs
│       └── Pages/
│           ├── _ValidationScriptsPartial.cshtml
│           └── _ViewStart.cshtml
├── Controllers/
│   ├── AccountController.cs
│   └── ChatController.cs
├── Models/
│   ├── Chat.cs
│   └── Message.cs
├── ViewModels/
│   └── AuthViewModel.cs
├── Views/
│   ├── Account/
│   │   └── RegLog.cshtml
│   ├── Chat/
│   │   ├── Chat.cshtml
│   │   ├── ChatHistory.cshtml
│   │   └── ChatMessages.cshtml
│   ├── Shared/
│   │   └── _Layout.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── appsettings.Development.json
├── appsettings.json
├── CodePilot.csproj
└── Program.cs
```

---

### 3. پوشه Areas/Identity

این پوشه مربوط به احراز هویت (Authentication) و مجوزها (Authorization) با استفاده از ASP.NET Identity است.

**3.1 ApplicationUser.cs:**
```csharp
public class ApplicationUser : IdentityUser
{
    // فیلد های پیش فرض برای ساخت جدول کاربران نظیر نام و نام خانوادگی...
}
```

> کلاس ApplicationUser از IdentityUser ارث‌بری می‌کند و برای ذخیره اطلاعات کاربر در پایگاه داده استفاده می‌شود.

**3.2 CodePilotContext.cs:**
```csharp
public class CodePilotContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }

    public CodePilotContext(DbContextOptions<CodePilotContext> options)
        : base(options)
    {
    }
}
```

> این کلاس کانتکست EF Core است که نمایانگر اتصال به پایگاه داده و نگاشت مدل‌ها (Chat و Message) به جداول است.

---

### 4. کلاس مدل ها (Models)

مدل‌ها نمایانگر ساختار داده‌ای هستند که در پایگاه داده ذخیره می‌شوند.

**4.1 Chat.cs:**
```csharp
public class Chat
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string UserId { get; set; }
    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastModified { get; set; }
    public ICollection<Message> Messages { get; set; }
}
```

* **Id**: شناسه یکتا برای هر گفتگو
* **Title**: عنوان گفتگو (معمولاً از پیام اول استخراج می‌شود)
* **UserId**: شناسه کاربری مالک گفتگو
* **User**: ناوبری به موجودیت ApplicationUser
* **CreatedAt**: زمان ایجاد گفتگو
* **LastModified**: زمان آخرین تغییر
* **Messages**: مجموعه پیام‌های مرتبط با این گفتگو

**4.2 Message.cs:**
```csharp
public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    [ForeignKey("ChatId")]
    public Chat Chat { get; set; }
    public bool IsUserMessage { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

* **Id**: شناسه یکتا برای پیام
* **ChatId**: شناسه گفتگو
* **Chat**: ناوبری به موجودیت Chat
* **IsUserMessage**: تعیین می‌کند که پیام از کاربر است یا پاسخ ربات
* **Content**: متن پیام
* **Timestamp**: زمان ارسال پیام

---
### 5. ViewModels

**AuthViewModel.cs:**
```csharp
public class AuthViewModel
{
    [Required]
    public string regUsername { get; set; }
    [Required]
    [EmailAddress]
    public string regEmail { get; set; }
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }

    // برای بخش ورود
    [Required]
    public string logUsername { get; set; }
}
```

> این کلاس برای تبادل داده بین فرم‌های ثبت‌نام و ورود و کنترلر Account استفاده می‌شود.

---

### 6. کنترلرها (Controllers)
**6.1 AccountController.cs**
این کنترلر مسئول ثبت‌نام، ورود، خروج و بررسی یکتا بودن نام کاربری و ایمیل است:
```csharp
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public IActionResult RegLog(string message)
    {
        ViewData["alert"] = message;
        return View();
    }

    public IActionResult CheckUsername(string regUsername)
    {
        var exists = _userManager.Users.Any(x => x.UserName == regUsername);
        return Json(!exists);
    }

    public IActionResult CheckEmail(string regEmail)
    {
        var exists = _userManager.Users.Any(x => x.Email == regEmail);
        return Json(!exists);
    }

    [HttpPost]
    public async Task<IActionResult> Register(AuthViewModel model)
    {
        var user = new ApplicationUser { UserName = model.regUsername, Email = model.regEmail };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var sent = await SendConfirmationEmail(user);
            var msg = sent ?
                "Registration successful! Confirm your Email to login." :
                "Error sending confirmation email. Please try again.";
            return RedirectToAction("RegLog", new { message = msg });
        }
        return RedirectToAction("RegLog", new { message = "Registration failed. Please try again." });
    }

    [HttpPost]
    public async Task<IActionResult> Login(AuthViewModel model)
    {
        var user = await _userManager.FindByNameAsync(model.logUsername);
        if (user == null) return RedirectToAction("RegLog", new { message = "Invalid credentials." });

        if (!user.EmailConfirmed)
        {
            await SendConfirmationEmail(user);
            return RedirectToAction("RegLog", new { message = "Email not confirmed. Please check your inbox." });
        }

        var result = await _signInManager.PasswordSignInAsync(model.logUsername, model.Password, true, true);
        if (result.Succeeded) return RedirectToAction("Chat", "Chat");

        return RedirectToAction("RegLog", new { message = "Invalid credentials." });
    }

    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("RegLog", new { message = "You have been logged out." });
    }

    // متد فرضی ارسال ایمیل تأیید
    private Task<bool> SendConfirmationEmail(ApplicationUser user)
    {
        // پیاده‌سازی ارسال ایمیل
        return Task.FromResult(true);
    }
}
```
* **ثبت‌نام**: دریافت مدل AuthViewModel و ایجاد ApplicationUser
* **ارسال ایمیل تأیید**: فراخوانی متد SendConfirmationEmail (تعریف نشده در کد نمونه)
* **ورود**: بررسی صحت اعتبارنامه‌ها و تأیید ایمیل
* **خروج**: SignOut و بازگشت به صفحه ثبت‌نام/ورود
* **بررسی یکتا بودن نام کاربری و ایمیل**: متدهای CheckUsername و CheckEmail با بازگشت JSON


 **6.2 ChatController.cs**
این کنترلر مسئول نمایش صفحه چت، بارگذاری تاریخچه، ارسال درخواست به API، دریافت و استریم پاسخ، ذخیره گفتگو و حذف آن است:
```csharp
[Authorize]
public class ChatController : Controller
{
    private readonly CodePilotContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(CodePilotContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public IActionResult Chat()
    {
        return View();
    }

    public async Task<IActionResult> ChatHistory()
    {
        var userId = (await _userManager.FindByNameAsync(User.Identity.Name)).Id;
        var chats = await _dbContext.Chats
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastModified)
            .ToListAsync();
        return View(chats);
    }

    public async Task<IActionResult> ChatMessages(int chatId)
    {
        var messages = await _dbContext.Messages
            .Where(m => m.ChatId == chatId)
            .ToListAsync();
        return View(messages);
    }

    [HttpPost]
    public async Task<IActionResult> GetChatResponse([FromBody] ChatRequest request)
    {
        Response.ContentType = "text/event-stream";

        // بارگذاری تاریخچه گفتگو
        var chatHistory = await _dbContext.Messages
            .Where(m => m.ChatId == request.ChatId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        var messages = chatHistory.Select(m => new { role = m.IsUserMessage ? "user" : "assistant", content = m.Content }).ToList();
        messages.Add(new { role = "user", content = request.Message });

        // ساخت payload برای HuggingFace
        var payload = new {
            model = "google/gemma-3-27b-it-fast",
            messages,
            max_tokens = 1024,
            temperature = 0.5,
            stream = true
        };

        // ارسال درخواست
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "<TOKEN_HERE>");
        var httpContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.SendAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://router.huggingface.co/nebius/v1/chat/completions") { Content = httpContent },
            HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // خواندن و استریم پاسخ
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        string? line; var botResponse = new StringBuilder();
        while ((line = await reader.ReadLineAsync()) != null) {
            if (!line.StartsWith("data: ")) continue;
            var json = JsonDocument.Parse(line.Substring(6));
            var delta = json.RootElement.GetProperty("choices")[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var contentEl)) {
                var content = contentEl.GetString();
                await Response.WriteAsync(content);
                await Response.Body.FlushAsync();
                botResponse.Append(content);
            }
        }

        // ذخیره پیام‌ها در پایگاه داده
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Chat chat;
        if (request.ChatId.HasValue)
        {
            chat = await _dbContext.Chats.FindAsync(request.ChatId.Value);
            chat.LastModified = DateTime.UtcNow;
        }
        else
        {
            chat = new Chat {
                UserId = userId,
                Title = TrimText(request.Message, 20),
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _dbContext.Chats.Add(chat);
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.Messages.AddRange(
            new Message { ChatId = chat.Id, IsUserMessage = true, Content = request.Message, Timestamp = DateTime.UtcNow },
            new Message { ChatId = chat.Id, IsUserMessage = false, Content = botResponse.ToString(), Timestamp = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        return new EmptyResult();
    }

    [HttpPost]
    public async Task<IActionResult> DeleteChat([FromBody] int chatId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var chat = await _dbContext.Chats.FirstOrDefaultAsync(c => c.Id == chatId && c.UserId == userId);
        if (chat == null) return NotFound();

        _dbContext.Chats.Remove(chat);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    private static string TrimText(string text, int maxLength) { /* ... */ }

    public class ChatRequest
    {
        public string Message { get; set; }
        public int? ChatId { get; set; }
    }
}
```
* **Chat()**: نمایش صفحه گفتگ
* **ChatHistory()**: بارگذاری لیست گفتگوهای کاربر
* **ChatMessages(int chatId)**: نمایش پیام‌های یک گفتگو
* **GetChatResponse()**: ارسال تاریخچه گفتگو به API HuggingFace و نمایش استریم پاسخ
* **DeleteChat()**: حذف یک گفتگو از پایگاه داده

**در متد GetChatResponse:**
1. خواندن پیام‌های قبلی از پایگاه
2. ساخت ساختار JSON برای ارسال به API مدل
3. خواندن پاسخ به صورت استریم و ارسال به کلاینت
4. ذخیره پیام کاربر و ربات در پایگاه
5. نماها (Razor Views)

---

### 7. نماها (Razor Views)

**7.1 RegLog.cshtml (ثبت‌نام/ورود):**
* دارای دو فرم تب‌بندی‌شده (Login و Register)
* استفاده از jQuery Validation برای اعتبارسنجی سمت کلاینت
* نمایش پیغام‌ها با ViewData\["alert"]

**7.2 Chat.cshtml (صفحه اصلی چت):**
* نوار کناری (Sidebar) برای نمایش تاریخچه گفتگوها و دکمه‌های تنظیمات و خروج
* بخش اصلی چت با Header، نمایش پیام‌ها و ورودی کاربر
* جاوااسکریپت:
	* بارگذاری تاریخچه گفتگو با AJAX
	* ارسال و دریافت پیام‌ها با Fetch API و پردازش استریم
	* هایلایت کد با Highlight.js
	* تنظیم خودکار ارتفاع textarea و تشخیص جهت متن

**7.3 ChatHistory.cshtml:**
* نمایش لیست گفتگوها به صورت آیتم‌های قابل کلیک برای بارگذاری پیام‌ها

**7.4 ChatMessages.cshtml:**
* نمایش پیام‌های یک گفتگو شامل الگوی HTML برای هر پیام (کاربر/ربات)

**7.5 Shared/\_Layout.cshtml:**
* ساختار اصلی HTML، لینک به CSS و اسکریپت‌های مشترک

---

### 8. سایر فایل ها
* **Program.cs**: پیکربندی سرویس‌ها (EF Core، Identity، MVC)
* **appsettings.json**: تنظیمات اتصال به پایگاه داده و دیگر گزینه‌ها
* **appsettings.Development.json**: تنظیمات مخصوص محیط توسعه

---
### 9. نتیجه‌گیری

این مستند به بررسی کاملی از اجزای پروژه CodePilot پرداخت. با مطالعه این مستند، توسعه‌دهنده می‌تواند درک کاملی از ساختار و جریان کار اپلیکیشن به‌دست آورد و در صورت نیاز، قابلیت‌ها را گسترش دهد.
