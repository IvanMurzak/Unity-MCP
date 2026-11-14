[English](DOCKER_DEPLOYMENT.md) | [中文](DOCKER_DEPLOYMENT.zh-CN.md) | [日本語](DOCKER_DEPLOYMENT.ja.md) | [Español](DOCKER_DEPLOYMENT.es.md) | [فارسی](DOCKER_DEPLOYMENT.fa.md) | <b>हिन्दी</b> | [العربية](DOCKER_DEPLOYMENT.ar.md)

# Docker डिप्लॉयमेंट

क्लाउड होस्टिंग या एक अलग-थलग एनवायरनमेंट के लिए MCP सर्वर को कंटेनर में चलाएँ। [README](README.hi.md) पर वापस जाएँ · [MCP सर्वर और कनेक्शन](mcp-server.hi.md)।

- **इमेज:** [`aigamedeveloper/mcp-server`](https://hub.docker.com/r/aigamedeveloper/mcp-server), साझा [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) रिपॉज़िटरी से बिल्ड की गई
- **टैग:** `latest` और रिलीज़ वर्ज़न `X.Y.Z`
- **पोर्ट:** `8080` (एक्सपोज़्ड)

आपको फिर भी अपने Unity प्रोजेक्ट में [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP#quick-start) प्लगइन चाहिए, जो सर्वर के URL के साथ **Custom** मोड पर सेट हो (देखें [कनेक्शन मोड](mcp-server.hi.md))।

![Docker launch](img/editor/docker-launch.gif)

## HTTP (सेल्फ-होस्टेड, कंटेनर के बाहर से पहुँचने योग्य)

सर्वर डिफ़ॉल्ट रूप से लूपबैक पर बाइंड होता है, जो पब्लिश किए गए पोर्ट के ज़रिए पहुँचने योग्य नहीं है। `MCP_BIND=any` सेट करें:

```bash
docker run -p 8080:8080 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp \
  -e MCP_PLUGIN_PORT=8080 \
  -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

एजेंट कॉन्फ़िग:

```json
{
  "mcpServers": {
    "ai-game-developer": {
      "url": "http://localhost:8080"
    }
  }
}
```

जब सर्वर कहीं और चलता हो, तो URL को अपने वास्तविक एंडपॉइंट से बदलें।

### साझा टोकन के साथ

`token` मोड में एजेंट और Unity प्लगइन (`UNITY_MCP_TOKEN`, या AI Game Developer विंडो का टोकन फ़ील्ड) दोनों से एक ही bearer सीक्रेट आवश्यक है। जब तक सर्वर के सामने TLS समाप्त न हो, सीक्रेट सादे HTTP पर जाता है।

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

### कस्टम पोर्ट

वही पोर्ट पब्लिश करें जो आपने `MCP_PLUGIN_PORT` में सेट किया है:

```bash
docker run -p 9090:9090 \
  -e MCP_PLUGIN_CLIENT_TRANSPORT=streamableHttp -e MCP_PLUGIN_PORT=9090 -e MCP_BIND=any \
  aigamedeveloper/mcp-server
```

## stdio (एजेंट कंटेनर शुरू करता है)

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

एजेंट कंटेनर के stdin और stdout के ज़रिए सर्वर से बात करता है; पब्लिश किया गया पोर्ट Unity प्लगइन के लिए है।

## कॉन्फ़िगरेशन

सभी सर्वर विकल्प एनवायरनमेंट वेरिएबल हैं (`-e NAME=value`)। सामान्य विकल्प [MCP सर्वर और कनेक्शन](mcp-server.hi.md) में सूचीबद्ध हैं; OAuth मोड और Redis-आधारित सत्रों सहित पूरी सूची [GameDev-MCP-Server README](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration) में है।
