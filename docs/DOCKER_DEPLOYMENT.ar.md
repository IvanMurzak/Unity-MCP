<div dir="rtl">

[English](DOCKER_DEPLOYMENT.md) | [中文](DOCKER_DEPLOYMENT.zh-CN.md) | [日本語](DOCKER_DEPLOYMENT.ja.md) | [Español](DOCKER_DEPLOYMENT.es.md) | [فارسی](DOCKER_DEPLOYMENT.fa.md) | [हिन्दी](DOCKER_DEPLOYMENT.hi.md) | <b>العربية</b>

# النشر باستخدام Docker

شغّل خادم MCP داخل حاوية، للاستضافة السحابية أو لبيئة معزولة. العودة إلى [README](README.ar.md) · [خادم MCP والاتصال](mcp-server.ar.md).

- **الصورة:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server)، مبنية من المستودع المشترك [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)
- **الوسوم:** `latest` وإصدارات الإطلاق `X.Y.Z`
- **المنفذ:** `8080` (مكشوف)

ما زلت بحاجة إلى إضافة [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) في مشروع Unity الخاص بك، مضبوطة على وضع **Custom** مع عنوان URL للخادم (راجع [أوضاع الاتصال](mcp-server.ar.md#أوضاع-الاتصال)).

</div>

![Docker launch](img/editor/docker-launch.gif)

<div dir="rtl">

## HTTP (مستضاف ذاتيًا، ويمكن الوصول إليه من خارج الحاوية)

يرتبط الخادم افتراضيًا بعنوان loopback، الذي لا يمكن الوصول إليه عبر منفذ منشور. اضبط `MCP_BIND=any`:

</div>

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

<div dir="rtl">

إعداد الوكيل:

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

استبدل عنوان URL بنقطة النهاية الحقيقية عندما يعمل الخادم في مكان آخر.

### مع رمز مشترك

يتطلب وضع `token` سرًّا واحدًا من نوع bearer يرسله كلٌّ من الوكيل وإضافة Unity (`UNITY_MCP_TOKEN`، أو حقل الرمز في نافذة AI Game Developer). يمرّ السر عبر HTTP غير مشفّر ما لم يُنهَ TLS أمام الخادم.

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

### منفذ مخصص

انشر المنفذ نفسه الذي ضبطته في `MCP_PLUGIN_PORT`:

</div>

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

<div dir="rtl">

## stdio (الوكيل يشغّل الحاوية)

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

يتواصل الوكيل مع الخادم عبر stdin وstdout الخاصين بالحاوية؛ أما المنفذ المنشور فهو لإضافة Unity.

## الإعداد

كل خيارات الخادم متغيرات بيئة (`-e NAME=value`). الخيارات الشائعة مدرجة في [خادم MCP والاتصال](mcp-server.ar.md#خيارات-الخادم-الشائعة)؛ والقائمة الكاملة، بما فيها وضع OAuth والجلسات المدعومة بـ Redis، موجودة في [README الخاص بـ GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration).

</div>
