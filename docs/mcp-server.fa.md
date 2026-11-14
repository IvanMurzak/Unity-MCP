<div dir="rtl">

[English](mcp-server.md) | [中文](mcp-server.zh-CN.md) | [日本語](mcp-server.ja.md) | [Español](mcp-server.es.md) | <b>فارسی</b> | [हिन्दी](mcp-server.hi.md) | [العربية](mcp-server.ar.md)

# سرور MCP و اتصال

اینکه پلاگین چگونه به یک سرور MCP می‌رسد، متغیرهای محیطی که تنظیمات آن را بازنویسی می‌کنند، و اینکه چگونه خودتان یک سرور اجرا کنید. بازگشت به [README](README.fa.md).

</div>

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

<div dir="rtl">

سرور در این مخزن نیست. این سرور همان **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** مشترک و مستقل از موتور است (فایل اجرایی `gamedev-mcp-server`، ایمیج Docker با نام `aigamedeveloper/mcp-server`) که Godot-MCP و Unreal-MCP هم از آن استفاده می‌کنند. README آن مرجع همه گزینه‌های سرور است. عامل از طریق `stdio` یا `streamableHttp` به سرور وصل می‌شود؛ پلاگین با SignalR به آن وصل می‌شود.

## حالت‌های اتصال

حالت در پنجره AI Game Developer (**Window ▸ AI Game Dev**) انتخاب می‌شود و در `UserSettings/AI-Game-Developer-Config.json` ذخیره می‌شود.

| حالت | سرور | توضیحات |
| --- | --- | --- |
| **Cloud** (پیش‌فرض) | `https://ai-game.dev/mcp` | با پنجره یا `unity-mcp-cli login` وارد شوید. هرگز سرور محلی را اجرا نمی‌کند، حتی اگر `keepServerRunning` برابر `true` باشد. |
| **Custom** | `gamedev-mcp-server` محلی، یا هر URLی که خودتان میزبانی می‌کنید | فیلد ذخیره‌شده `host` فقط در این حالت استفاده می‌شود. برای استفاده از سرور محلی، در پنجره **Custom** را انتخاب کنید، یا در حالی که Unity بسته است `"connectionMode": "Custom"` را تنظیم کنید. |

در حالت Custom با یک سرور محلی، پلاگین نسخه‌ای از سرور را که با `ServerVersion` در `McpServerManager.cs` پین شده است در `Library/mcp-server/<rid>/` دانلود و اجرا می‌کند. منوی **Tools ▸ AI Game Developer ▸ Server** گزینه‌های *Download Binaries* و *Open Server Logs* را دارد.

## متغیرهای محیطی پلاگین

هنگام راه‌اندازی ویرایشگر خوانده می‌شوند. این متغیرها پیکربندی ذخیره‌شده را **فقط در حافظه** بازنویسی می‌کنند و هرگز در آن نوشته نمی‌شوند، بنابراین پنجره همچنان انتخاب ذخیره‌شده را نشان می‌دهد. آرگومان‌های خط فرمان بر متغیرهای محیطی اولویت دارند. هر متغیر به‌صورت آرگومان خط فرمان `-NAME=value` هم پذیرفته می‌شود (برای مثال `-UNITY_MCP_TRANSPORT=stdio`)؛ `-url`، `-auth` و `-token` نام‌های مستعار کوتاه هستند. ردیف‌هایی که با *env only* علامت خورده‌اند آرگومانی ندارند.

| متغیر | مقادیر | اثر |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | آدرس سرور MCP (`UNITY_MCP_HOST` یک نام مستعار قدیمی است) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | حالت اتصال (یک URL loopback به معنای `Custom` است) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | فعال نگه داشتن اتصال |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | حالت احراز هویت (مقدار قدیمی `required` به `token` تبدیل می‌شود) |
| `UNITY_MCP_TOKEN` | رشته | توکن احراز هویت |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | transport کلاینت که پلاگین پیکربندی می‌کند |
| `UNITY_MCP_START_SERVER` | `true` / `false` | در حال اجرا نگه داشتن فرایند سرور محلی |
| `UNITY_MCP_TOOLS` | شناسه‌های ابزار جداشده با کاما | فقط این ابزارها نمایش داده می‌شوند؛ بقیه پنهان می‌شوند (همچنان با نام قابل فراخوانی‌اند). شناسه‌های ناشناخته به‌عنوان خطا ثبت می‌شوند. |
| `UNITY_MCP_CREDENTIALS_DIR` | مسیر مطلق پوشه | *env only.* مخزن اعتبارنامه ماشین را به‌جای `~/.ai-game-dev` از این پوشه بخوان (ویرایشگرهای توسعه و تست). مقدار نسبی نادیده گرفته می‌شود. |
| `UNITY_MCP_SERVER_PATH` | مسیر مطلق یک `gamedev-mcp-server` | *env only.* این فایل اجرایی را به‌جای نسخه پین‌شده اجرا کن (پایین را ببینید) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *env only.* از مقداردهی اولیه پلاگین صرف‌نظر کن؛ [غیرفعال‌سازی در CI](ci-disable.fa.md) را ببینید |

مثال، حالت batch در برابر یک سرور محلی با توکن:

</div>

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

<div dir="rtl">

### `UNITY_MCP_SERVER_PATH` (توسعه سرور و CI)

ویرایشگر به‌جای نسخه پین‌شده، فایل نام‌برده را اجرا می‌کند و از دانلود و بررسی نسخه صرف‌نظر می‌کند. از آن هنگام توسعه سرور یا زمانی که CI سرور را از سورس build می‌کند استفاده کنید.

- ابتدا از محیط فرایند و سپس از فایل `.env` در ریشه پروژه Unity خوانده می‌شود، تا ویرایشگری که از GUI یا یک IDE اجرا شده هم بتواند آن را دریافت کند.
- از مسیر **مطلق** استفاده کنید. اگر فایل وجود نداشته باشد، پلاگین بی‌صدا طوری رفتار می‌کند که انگار متغیر تنظیم نشده است؛ خط کنسول `Starting MCP server: <path> …` نام فایل اجرایی‌ای را که واقعاً اجرا شده نشان می‌دهد.
- این بازنویسی همچنین به `command` در پیکربندی‌های تولیدشده عامل تبدیل می‌شود، و *Open Server Logs* هم از آن پیروی می‌کند. *Download Binaries* همچنان در `Library/mcp-server/<rid>/` دانلود می‌کند.

## اجرای سرور توسط خودتان

از این روش برای راه‌اندازی خودمیزبان یا راه دور استفاده کنید. یک فایل zip نسخه (`gamedev-mcp-server-<rid>.zip`) را از [نسخه‌های GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server/releases) دانلود کنید، یا از [Docker](DOCKER_DEPLOYMENT.fa.md) استفاده کنید.

</div>

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

<div dir="rtl">

پیکربندی عامل برای stdio (مسیر را جایگزین کنید؛ در Windows نام فایل به `.exe` ختم می‌شود):

</div>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "<project>/Library/mcp-server/win-x64/gamedev-mcp-server.exe",
      "args": ["--port=8080", "--client-transport=stdio"]
    }
  }
}
```

<div dir="rtl">

پلاگین باید از همان پورت استفاده کند: در پنجره AI Game Developer حالت **Custom** و همان host و پورت را تنظیم کنید.

### گزینه‌های رایج سرور

آرگومان‌های خط فرمان متغیرهای محیطی را بازنویسی می‌کنند. آرگومان‌ها با یک `-` یا بدون آن (`port=8080`) هم کار می‌کنند. مقادیر پیش‌فرض و فهرست کامل: [پیکربندی GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).

| متغیر محیطی | آرگومان | معنا |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | پورت هم برای عامل (HTTP) و هم برای پلاگین (پیش‌فرض `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | transport عامل: `stdio` یا `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | مهلت درخواست‌های کاتالوگ، پرامپت و منبع به پلاگین (میلی‌ثانیه) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | حداکثر زمان اجرای یک فراخوانی ابزار (میلی‌ثانیه). هر فراخوانی یک بار فرستاده می‌شود و هرگز دوباره ارسال نمی‌شود. |
| `MCP_AUTH` (یا `MCP_AUTHORIZATION`) | `--auth` (یا `--authorization`) | `none`، `token` یا `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | راز مشترک برای حالت `token`؛ عامل و پلاگین هر دو باید آن را بفرستند |
| `MCP_BIND` | `--bind` | آدرس bind: `loopback` (پیش‌فرض)، `any`، یا یک IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | بستن نشست‌های streamableHttp بیکار پس از این تعداد ثانیه |

استقرارهای میزبانی‌شده همچنین می‌توانند در رویدادهای ابزار، پرامپت، منبع و اتصال webhook فراخوانی کنند و اتصال‌های ورودی را مجاز کنند: `MCP_PLUGIN_WEBHOOK_TOOL_URL`، `MCP_PLUGIN_WEBHOOK_PROMPT_URL`، `MCP_PLUGIN_WEBHOOK_RESOURCE_URL`، `MCP_PLUGIN_WEBHOOK_CONNECTION_URL`، `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`، `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`، `MCP_PLUGIN_WEBHOOK_TOKEN`، `MCP_PLUGIN_WEBHOOK_HEADER` و `MCP_PLUGIN_WEBHOOK_TIMEOUT` (همه اختیاری، به‌طور پیش‌فرض تنظیم‌نشده).

</div>
