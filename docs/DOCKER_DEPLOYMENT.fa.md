<div dir="rtl">

[English](DOCKER_DEPLOYMENT.md) | [中文](DOCKER_DEPLOYMENT.zh-CN.md) | [日本語](DOCKER_DEPLOYMENT.ja.md) | [Español](DOCKER_DEPLOYMENT.es.md) | <b>فارسی</b> | [हिन्दी](DOCKER_DEPLOYMENT.hi.md) | [العربية](DOCKER_DEPLOYMENT.ar.md)

# استقرار با Docker

سرور MCP را در یک کانتینر اجرا کنید، برای میزبانی ابری یا یک محیط ایزوله. بازگشت به [README](README.fa.md) · [سرور MCP و اتصال](mcp-server.fa.md).

- **ایمیج:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server)، ساخته‌شده از مخزن مشترک [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)
- **تگ‌ها:** `latest` و نسخه‌های انتشار `X.Y.Z`
- **پورت:** `8080` (expose شده)

همچنان به پلاگین [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) در پروژه Unity خود نیاز دارید، که روی حالت **Custom** با URL سرور تنظیم شده باشد ([حالت‌های اتصال](mcp-server.fa.md) را ببینید).

</div>

![Docker launch](img/editor/docker-launch.gif)

<div dir="rtl">

## HTTP (خودمیزبان، قابل دسترس از بیرون کانتینر)

سرور به‌طور پیش‌فرض به loopback متصل (bind) می‌شود که از طریق یک پورت منتشرشده قابل دسترس نیست. `MCP_BIND=any` را تنظیم کنید:

</div>

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

<div dir="rtl">

پیکربندی عامل:

</div>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

<div dir="rtl">

وقتی سرور جای دیگری اجرا می‌شود، URL را با endpoint واقعی خود جایگزین کنید.

### با یک توکن مشترک

حالت `token` یک راز bearer واحد را هم از عامل و هم از پلاگین Unity می‌خواهد (`UNITY_MCP_TOKEN`، یا فیلد توکن در پنجره AI Game Developer). این راز از طریق HTTP ساده عبور می‌کند، مگر اینکه TLS جلوی سرور خاتمه یابد.

</div>

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=8080 -e MCP_BIND=any \
  -e MCP_AUTH=token -e MCP_PLUGIN_TOKEN=<secret> \
  aigamedeveloper/mcp-server
```

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080",
      "headers": { "Authorization": "Bearer <secret>" }
    }
  }
}
```

<div dir="rtl">

### پورت سفارشی

همان پورتی را که در `MCP_PLUGIN_PORT` تنظیم کرده‌اید منتشر کنید:

</div>

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

<div dir="rtl">

## stdio (عامل کانتینر را اجرا می‌کند)

</div>

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-e", "MCP_PLUGIN_CLIENT_TRANSPORT=stdio",
        "-e", "MCP_BIND=any",
        "-p", "8080:8080",
        "aigamedeveloper/mcp-server"
      ]
    }
  }
}
```

<div dir="rtl">

عامل از طریق stdin و stdout کانتینر با سرور صحبت می‌کند؛ پورت منتشرشده برای پلاگین Unity است.

## پیکربندی

همه گزینه‌های سرور متغیرهای محیطی هستند (`-e NAME=value`). موارد رایج در [سرور MCP و اتصال](mcp-server.fa.md) فهرست شده‌اند؛ فهرست کامل، شامل حالت OAuth و نشست‌های مبتنی بر Redis، در [README سرور GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration) آمده است.

</div>
