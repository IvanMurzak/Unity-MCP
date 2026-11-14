<div dir="rtl">

[English](mcp-server.md) | [中文](mcp-server.zh-CN.md) | [日本語](mcp-server.ja.md) | [Español](mcp-server.es.md) | [فارسی](mcp-server.fa.md) | [हिन्दी](mcp-server.hi.md) | <b>العربية</b>

# خادم MCP والاتصال

كيف تصل الإضافة إلى خادم MCP، ومتغيرات البيئة التي تتجاوز إعداداتها، وكيف تشغّل خادمًا بنفسك. العودة إلى [README](README.ar.md).

<div align="center">

‏`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

الخادم ليس موجودًا في هذا المستودع. إنه الخادم المشترك المستقل عن المحرك **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** (الملف التنفيذي `gamedev-mcp-server`، وصورة Docker `aigamedeveloper/mcp-server`)، ويستخدمه أيضًا Godot-MCP وUnreal-MCP. ملف README الخاص به هو المرجع لكل خيارات الخادم. يتصل الوكيل بالخادم عبر `stdio` أو `streamableHttp`؛ وتتصل به الإضافة عبر SignalR.

## أوضاع الاتصال

يُختار الوضع في نافذة AI Game Developer (**Window ▸ AI Game Dev**) ويُحفظ في `UserSettings/AI-Game-Developer-Config.json`.

| الوضع | الخادم | ملاحظات |
| --- | --- | --- |
| **Cloud** (افتراضي) | `https://ai-game.dev/mcp` | سجّل الدخول من النافذة أو بالأمر `unity-mcp-cli login`. لا يشغّل خادمًا محليًا أبدًا، حتى لو كانت `keepServerRunning` تساوي `true`. |
| **Custom** | `gamedev-mcp-server` محلي، أو أي عنوان URL تستضيفه | يُستخدم الحقل المحفوظ `host` في هذا الوضع فقط. لاستخدام خادم محلي، اختر **Custom** في النافذة، أو اضبط `"connectionMode": "Custom"` بينما Unity مغلق. |

في وضع Custom مع خادم محلي، تنزّل الإضافة إصدار الخادم المثبّت بواسطة `ServerVersion` في `McpServerManager.cs` إلى `Library/mcp-server/<rid>/` وتشغّله. تحتوي القائمة **Tools ▸ AI Game Developer ▸ Server** على *Download Binaries* و*Open Server Logs*.

## متغيرات بيئة الإضافة

تُقرأ عند بدء تشغيل المحرر. وهي تتجاوز الإعداد المحفوظ **في الذاكرة فقط** ولا تُكتب أبدًا، لذا تظل النافذة تعرض الخيار المحفوظ. وسيطات سطر الأوامر تتقدّم على متغيرات البيئة. ويُقبل كل متغير أيضًا كوسيط سطر أوامر `-NAME=value` (مثلًا `-UNITY_MCP_TRANSPORT=stdio`)؛ و`-url` و`-auth` و`-token` أسماء مختصرة. الصفوف المعلّمة بـ *env only* ليس لها وسيط.

| المتغير | القيم | الأثر |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | عنوان URL لخادم MCP (`UNITY_MCP_HOST` اسم بديل قديم) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | وضع الاتصال (عنوان loopback يعني ضمنًا `Custom`) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | إبقاء الاتصال نشطًا |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | وضع المصادقة (القيمة القديمة `required` تصبح `token`) |
| `UNITY_MCP_TOKEN` | سلسلة نصية | رمز المصادقة |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | نقل العميل الذي تضبطه الإضافة |
| `UNITY_MCP_START_SERVER` | `true` / `false` | إبقاء عملية خادم محلي قيد التشغيل |
| `UNITY_MCP_TOOLS` | معرّفات أدوات مفصولة بفواصل | إظهار هذه الأدوات فقط؛ وتُخفى البقية (مع بقائها قابلة للاستدعاء بالاسم). تُسجَّل المعرّفات غير المعروفة كأخطاء. |
| `UNITY_MCP_CREDENTIALS_DIR` | مسار مجلد مطلق | *env only.* قراءة مخزن بيانات اعتماد الجهاز من هذا المجلد بدلًا من `~/.ai-game-dev` (لمحررات التطوير والاختبار). تُتجاهل القيمة النسبية. |
| `UNITY_MCP_SERVER_PATH` | مسار مطلق إلى `gamedev-mcp-server` | *env only.* تشغيل هذا الملف التنفيذي بدلًا من الإصدار المثبّت (انظر أدناه) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *env only.* تخطي تهيئة الإضافة؛ راجع [التعطيل في CI](ci-disable.ar.md) |

مثال، وضع الدُفعات (batch mode) مقابل خادم محلي مع رمز:

</div>

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

<div dir="rtl">

### `UNITY_MCP_SERVER_PATH` (تطوير الخادم وCI)

يشغّل المحرر الملف المحدد بدلًا من الإصدار المثبّت ويتخطى التنزيل وفحص الإصدار. استخدمه عند تطوير الخادم أو عندما يبني CI الخادم من المصدر.

- يُقرأ من بيئة العملية، ثم من ملف `.env` في جذر مشروع Unity، كي يتمكن محرر بدأ من الواجهة الرسومية أو من IDE من التقاطه.
- استخدم مسارًا **مطلقًا**. إذا لم يكن الملف موجودًا، تتصرف الإضافة بصمت كأن المتغير غير مضبوط؛ ويذكر سطر وحدة التحكم `Starting MCP server: <path> …` الملف التنفيذي الذي شُغّل فعلًا.
- يصبح هذا التجاوز أيضًا قيمة `command` في إعدادات الوكلاء المولَّدة، ويتبعه *Open Server Logs*. أما *Download Binaries* فما زال ينزّل إلى `Library/mcp-server/<rid>/`.

## تشغيل خادم بنفسك

استخدم هذا لإعداد مستضاف ذاتيًا أو بعيد. نزّل ملف zip لإصدار (`gamedev-mcp-server-<rid>.zip`) من [إصدارات GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server/releases)، أو استخدم [Docker](DOCKER_DEPLOYMENT.ar.md).

</div>

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

<div dir="rtl">

إعداد الوكيل لـ stdio (استبدل المسار؛ على Windows ينتهي الملف بـ `.exe`):

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

يجب أن تستخدم الإضافة المنفذ نفسه: اضبط وضع **Custom** والمضيف والمنفذ نفسيهما في نافذة AI Game Developer.

### خيارات الخادم الشائعة

وسيطات سطر الأوامر تتجاوز متغيرات البيئة. وتعمل الوسيطات أيضًا بشرطة واحدة `-` أو بدونها (`port=8080`). القيم الافتراضية والقائمة الكاملة: [إعداد GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).

| متغير البيئة | الوسيط | المعنى |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | المنفذ لكلٍّ من الوكيل (HTTP) والإضافة (الافتراضي `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | نقل الوكيل: `stdio` أو `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | مهلة طلبات الفهرس والموجّهات والموارد إلى الإضافة (بالمللي ثانية) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | المدة التي قد يستغرقها استدعاء أداة واحد (بالمللي ثانية). يُرسل الاستدعاء مرة واحدة ولا يُعاد إرساله أبدًا. |
| `MCP_AUTH` (أو `MCP_AUTHORIZATION`) | `--auth` (أو `--authorization`) | `none` أو `token` أو `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | السر المشترك لوضع `token`؛ ويجب أن يرسله كلٌّ من الوكيل والإضافة |
| `MCP_BIND` | `--bind` | عنوان الربط: `loopback` (افتراضي)، أو `any`، أو عنوان IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | إغلاق جلسات streamableHttp الخاملة بعد هذا العدد من الثواني |

يمكن لعمليات النشر المستضافة أيضًا استدعاء webhooks عند أحداث الأدوات والموجّهات والموارد والاتصال، وتفويض الاتصالات الواردة: `MCP_PLUGIN_WEBHOOK_TOOL_URL` و`MCP_PLUGIN_WEBHOOK_PROMPT_URL` و`MCP_PLUGIN_WEBHOOK_RESOURCE_URL` و`MCP_PLUGIN_WEBHOOK_CONNECTION_URL` و`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL` و`MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN` و`MCP_PLUGIN_WEBHOOK_TOKEN` و`MCP_PLUGIN_WEBHOOK_HEADER` و`MCP_PLUGIN_WEBHOOK_TIMEOUT` (جميعها اختيارية وغير مضبوطة افتراضيًا).

</div>
