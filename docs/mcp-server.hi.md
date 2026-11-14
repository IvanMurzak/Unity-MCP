[English](mcp-server.md) | [中文](mcp-server.zh-CN.md) | [日本語](mcp-server.ja.md) | [Español](mcp-server.es.md) | [فارسی](mcp-server.fa.md) | <b>हिन्दी</b> | [العربية](mcp-server.ar.md)

# MCP सर्वर और कनेक्शन

प्लगइन किसी MCP सर्वर तक कैसे पहुँचता है, कौन-से एनवायरनमेंट वेरिएबल उसकी सेटिंग्स को ओवरराइड करते हैं, और खुद सर्वर कैसे चलाएँ। [README](README.hi.md) पर वापस जाएँ।

<div align="center">

`AI agent` ↔ **`MCP server`** ↔ `Unity plugin (Editor or game)`

</div>

सर्वर इस रिपॉज़िटरी में नहीं है। यह साझा, इंजन-स्वतंत्र **[GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)** है (बाइनरी `gamedev-mcp-server`, Docker इमेज `aigamedeveloper/mcp-server`), जिसका उपयोग Godot-MCP और Unreal-MCP भी करते हैं। हर सर्वर विकल्प के लिए उसका README ही संदर्भ है। एजेंट सर्वर से `stdio` या `streamableHttp` पर कनेक्ट होता है; प्लगइन उससे SignalR के ज़रिए कनेक्ट होता है।

## कनेक्शन मोड

मोड AI Game Developer विंडो (**Window ▸ AI Game Dev**) में चुना जाता है और `UserSettings/AI-Game-Developer-Config.json` में सहेजा जाता है।

| मोड | सर्वर | टिप्पणियाँ |
| --- | --- | --- |
| **Cloud** (डिफ़ॉल्ट) | `https://ai-game.dev/mcp` | विंडो से या `unity-mcp-cli login` से साइन इन करें। कभी लोकल सर्वर शुरू नहीं करता, भले ही `keepServerRunning` `true` हो। |
| **Custom** | लोकल `gamedev-mcp-server`, या कोई भी URL जिसे आप होस्ट करते हैं | सहेजा गया `host` फ़ील्ड केवल इसी मोड में उपयोग होता है। लोकल सर्वर का उपयोग करने के लिए विंडो में **Custom** चुनें, या Unity बंद होने पर `"connectionMode": "Custom"` सेट करें। |

लोकल सर्वर के साथ Custom मोड में, प्लगइन `McpServerManager.cs` में `ServerVersion` द्वारा पिन की गई सर्वर रिलीज़ को `Library/mcp-server/<rid>/` में डाउनलोड करता है और उसे शुरू करता है। **Tools ▸ AI Game Developer ▸ Server** में *Download Binaries* और *Open Server Logs* हैं।

## प्लगइन एनवायरनमेंट वेरिएबल

Editor स्टार्टअप पर पढ़े जाते हैं। ये सहेजे गए कॉन्फ़िग को **केवल मेमोरी में** ओवरराइड करते हैं और कभी वापस नहीं लिखे जाते, इसलिए विंडो फिर भी सहेजा गया विकल्प दिखाती है। कमांड-लाइन आर्ग्युमेंट एनवायरनमेंट वेरिएबल पर प्राथमिकता पाते हैं। हर वेरिएबल कमांड-लाइन आर्ग्युमेंट `-NAME=value` के रूप में भी स्वीकार किया जाता है (उदाहरण के लिए `-UNITY_MCP_TRANSPORT=stdio`); `-url`, `-auth` और `-token` छोटे उपनाम हैं। *केवल env* से चिह्नित पंक्तियों का कोई आर्ग्युमेंट नहीं है।

| वेरिएबल | मान | प्रभाव |
| --- | --- | --- |
| `UNITY_MCP_CLOUD_URL` | URL | MCP सर्वर URL (`UNITY_MCP_HOST` एक पुराना उपनाम है) |
| `UNITY_MCP_CONNECTION_MODE` | `Cloud` / `Custom` | कनेक्शन मोड (लूपबैक URL का अर्थ `Custom` है) |
| `UNITY_MCP_KEEP_CONNECTED` | `true` / `false` | कनेक्शन को सक्रिय रखें |
| `UNITY_MCP_AUTH_OPTION` | `none` / `oauth` / `token` | प्रमाणीकरण मोड (पुराना `required` `token` बन जाता है) |
| `UNITY_MCP_TOKEN` | string | प्रमाणीकरण टोकन |
| `UNITY_MCP_TRANSPORT` | `stdio` / `streamableHttp` | क्लाइंट ट्रांसपोर्ट जिसे प्लगइन कॉन्फ़िगर करता है |
| `UNITY_MCP_START_SERVER` | `true` / `false` | एक लोकल सर्वर प्रोसेस चालू रखें |
| `UNITY_MCP_TOOLS` | अल्पविराम से अलग tool ids | केवल ये टूल्स दिखाएँ; बाकी छिपे रहते हैं (फिर भी नाम से कॉल किए जा सकते हैं)। अज्ञात ids एरर के रूप में लॉग होते हैं। |
| `UNITY_MCP_CREDENTIALS_DIR` | पूर्ण फ़ोल्डर पाथ | *केवल env।* मशीन क्रेडेंशियल स्टोर को `~/.ai-game-dev` के बजाय इस फ़ोल्डर से पढ़ें (डेवलपमेंट और टेस्ट Editors)। सापेक्ष मान को अनदेखा किया जाता है। |
| `UNITY_MCP_SERVER_PATH` | किसी `gamedev-mcp-server` का पूर्ण पाथ | *केवल env।* पिन की गई रिलीज़ के बजाय यह बाइनरी लॉन्च करें (नीचे देखें) |
| `UNITY_MCP_DISABLE` | `1` / `true` | *केवल env।* प्लगइन इनिशियलाइज़ेशन छोड़ दें; देखें [CI में अक्षम करें](ci-disable.hi.md) |

उदाहरण, टोकन के साथ लोकल सर्वर के विरुद्ध बैच मोड:

```bash
Unity.exe -batchmode -nographics \
  -UNITY_MCP_CLOUD_URL=http://localhost:8080 \
  -UNITY_MCP_KEEP_CONNECTED=true \
  -UNITY_MCP_AUTH_OPTION=token \
  -UNITY_MCP_TOKEN=my-secret-token
```

### `UNITY_MCP_SERVER_PATH` (सर्वर डेवलपमेंट और CI)

Editor पिन की गई रिलीज़ के बजाय नामित फ़ाइल लॉन्च करता है और डाउनलोड तथा वर्ज़न जाँच को छोड़ देता है। इसका उपयोग तब करें जब आप सर्वर विकसित कर रहे हों या जब CI सर्वर को सोर्स से बिल्ड करता हो।

- पहले प्रोसेस एनवायरनमेंट से, फिर Unity प्रोजेक्ट रूट की `.env` फ़ाइल से पढ़ा जाता है, ताकि GUI या IDE से शुरू किया गया Editor भी इसे उठा सके।
- **पूर्ण (absolute)** पाथ का उपयोग करें। यदि फ़ाइल मौजूद नहीं है, तो प्लगइन चुपचाप ऐसा व्यवहार करता है मानो वेरिएबल सेट ही न हो; कंसोल पंक्ति `Starting MCP server: <path> …` उस बाइनरी का नाम बताती है जो वास्तव में लॉन्च हुई।
- यह ओवरराइड जनरेट किए गए एजेंट कॉन्फ़िग्स में `command` भी बन जाता है, और *Open Server Logs* भी इसका अनुसरण करता है। *Download Binaries* फिर भी `Library/mcp-server/<rid>/` में डाउनलोड करता है।

## खुद सर्वर चलाएँ

सेल्फ-होस्टेड या रिमोट सेटअप के लिए इसका उपयोग करें। [GameDev-MCP-Server releases](https://github.com/IvanMurzak/GameDev-MCP-Server/releases) से एक रिलीज़ zip (`gamedev-mcp-server-<rid>.zip`) डाउनलोड करें, या [Docker](DOCKER_DEPLOYMENT.hi.md) का उपयोग करें।

```bash
# stdio: the agent starts the server itself
./gamedev-mcp-server --port 8080 --client-transport stdio

# HTTP: run it as a standalone process
./gamedev-mcp-server --port 8080 --client-transport streamableHttp
```

stdio के लिए एजेंट कॉन्फ़िग (पाथ बदलें; Windows पर फ़ाइल `.exe` पर समाप्त होती है):

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

प्लगइन को वही पोर्ट उपयोग करना चाहिए: AI Game Developer विंडो में **Custom** मोड और वही होस्ट तथा पोर्ट सेट करें।

### सामान्य सर्वर विकल्प

कमांड-लाइन आर्ग्युमेंट एनवायरनमेंट वेरिएबल को ओवरराइड करते हैं। आर्ग्युमेंट एक `-` के साथ या बिना किसी के भी काम करते हैं (`port=8080`)। डिफ़ॉल्ट और पूरी सूची: [GameDev-MCP-Server configuration](https://github.com/IvanMurzak/GameDev-MCP-Server#configuration)।

| एनवायरनमेंट वेरिएबल | आर्ग्युमेंट | अर्थ |
| --- | --- | --- |
| `MCP_PLUGIN_PORT` | `--port` | एजेंट (HTTP) और प्लगइन दोनों के लिए पोर्ट (डिफ़ॉल्ट `8080`) |
| `MCP_PLUGIN_CLIENT_TRANSPORT` | `--client-transport` | एजेंट ट्रांसपोर्ट: `stdio` या `streamableHttp` |
| `MCP_PLUGIN_CLIENT_TIMEOUT` | `--plugin-timeout` | प्लगइन को कैटलॉग, प्रॉम्प्ट और रिसोर्स अनुरोधों के लिए टाइमआउट (ms) |
| `MCP_PLUGIN_TOOL_TIMEOUT` | `--tool-timeout` | एक टूल कॉल कितनी देर तक चल सकती है (ms)। कॉल एक बार भेजी जाती है और कभी दोबारा नहीं भेजी जाती। |
| `MCP_AUTH` (या `MCP_AUTHORIZATION`) | `--auth` (या `--authorization`) | `none`, `token` या `oauth` |
| `MCP_PLUGIN_TOKEN` | `--token` | `token` मोड के लिए साझा सीक्रेट; एजेंट और प्लगइन दोनों को इसे भेजना होगा |
| `MCP_BIND` | `--bind` | बाइंड पता: `loopback` (डिफ़ॉल्ट), `any`, या एक IP |
| `MCP_PLUGIN_IDLE_TIMEOUT_SECONDS` | `--idle-timeout-seconds` | निष्क्रिय streamableHttp सत्रों को इतने सेकंड बाद बंद करें |

होस्टेड डिप्लॉयमेंट टूल, प्रॉम्प्ट, रिसोर्स और कनेक्शन इवेंट्स पर वेबहुक भी कॉल कर सकते हैं और आने वाले कनेक्शनों को अधिकृत कर सकते हैं: `MCP_PLUGIN_WEBHOOK_TOOL_URL`, `MCP_PLUGIN_WEBHOOK_PROMPT_URL`, `MCP_PLUGIN_WEBHOOK_RESOURCE_URL`, `MCP_PLUGIN_WEBHOOK_CONNECTION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_URL`, `MCP_PLUGIN_WEBHOOK_AUTHORIZATION_FAIL_OPEN`, `MCP_PLUGIN_WEBHOOK_TOKEN`, `MCP_PLUGIN_WEBHOOK_HEADER` और `MCP_PLUGIN_WEBHOOK_TIMEOUT` (सभी वैकल्पिक, डिफ़ॉल्ट रूप से सेट नहीं)।
