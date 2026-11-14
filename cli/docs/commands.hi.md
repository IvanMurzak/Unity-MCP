[English](commands.md) | [中文](commands.zh-CN.md) | [日本語](commands.ja.md) | [Español](commands.es.md) | [فارسی](commands.fa.md) | <b>हिन्दी</b> | [العربية](commands.ar.md)

# unity-mcp-cli संदर्भ

[`unity-mcp-cli`](../README.md) की हर कमांड और विकल्प, एग्ज़िट कोड, मशीन-पठनीय आउटपुट और लाइब्रेरी API।
टर्मिनल में यही विवरण पाने के लिए `unity-mcp-cli <command> --help` चलाएँ।

**कमांड्स:** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands-1) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**यह भी:** [ग्लोबल विकल्प](#global-options) · [एग्ज़िट कोड और मशीन-पठनीय आउटपुट](#exit-codes-and-machine-readable-output) · [पूर्ण ऑटोमेशन उदाहरण](#full-automation-example) · [यह कैसे काम करता है](#how-it-works) · [लाइब्रेरी API](#library-api)

# Commands

## `configure`

`UserSettings/AI-Game-Developer-Config.json` में MCP टूल्स, प्रॉम्प्ट्स और रिसोर्सेज़ कॉन्फ़िगर करें।

```bash
unity-mcp-cli configure ./MyGame --list
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | हाँ | Unity प्रोजेक्ट का पाथ (positional या `--path`) |
| `--list` | नहीं | वर्तमान कॉन्फ़िगरेशन सूचीबद्ध करें और बाहर निकलें |
| `--enable-tools <names>` | नहीं | विशिष्ट टूल्स MCP क्लाइंट्स को दिखाएँ (अल्पविराम से अलग) |
| `--disable-tools <names>` | नहीं | विशिष्ट टूल्स MCP क्लाइंट्स से छिपाएँ (अल्पविराम से अलग; छिपा हुआ टूल नाम से कॉल किए जाने पर फिर भी चलता है) |
| `--enable-all-tools` | नहीं | सभी टूल्स दिखाएँ |
| `--disable-all-tools` | नहीं | सभी टूल्स छिपाएँ |
| `--enable-prompts <names>` | नहीं | विशिष्ट प्रॉम्प्ट्स MCP क्लाइंट्स को दिखाएँ (अल्पविराम से अलग) |
| `--disable-prompts <names>` | नहीं | विशिष्ट प्रॉम्प्ट्स MCP क्लाइंट्स से छिपाएँ (अल्पविराम से अलग; छिपा हुआ टूल नाम से कॉल किए जाने पर फिर भी चलता है) |
| `--enable-all-prompts` | नहीं | सभी प्रॉम्प्ट्स दिखाएँ |
| `--disable-all-prompts` | नहीं | सभी प्रॉम्प्ट्स छिपाएँ |
| `--enable-resources <names>` | नहीं | विशिष्ट रिसोर्सेज़ MCP क्लाइंट्स को दिखाएँ (अल्पविराम से अलग) |
| `--disable-resources <names>` | नहीं | विशिष्ट रिसोर्सेज़ MCP क्लाइंट्स से छिपाएँ (अल्पविराम से अलग; छिपा हुआ टूल नाम से कॉल किए जाने पर फिर भी चलता है) |
| `--enable-all-resources` | नहीं | सभी रिसोर्सेज़ दिखाएँ |
| `--disable-all-resources` | नहीं | सभी रिसोर्सेज़ छिपाएँ |

**उदाहरण — विशिष्ट टूल्स सक्षम करें और सभी प्रॉम्प्ट्स अक्षम करें:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

**उदाहरण — सब कुछ सक्षम करें:**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

## `create-project`

Unity Editor का उपयोग करके एक नया Unity प्रोजेक्ट बनाएँ।

```bash
unity-mcp-cli create-project /path/to/new/project
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | हाँ | वह पाथ जहाँ प्रोजेक्ट बनाया जाएगा (positional या `--path`) |
| `--unity <version>` | नहीं | उपयोग करने के लिए Unity Editor वर्ज़न (डिफ़ॉल्ट रूप से सबसे ऊँचा इंस्टॉल किया गया) |

**उदाहरण — किसी विशिष्ट एडिटर वर्ज़न के साथ प्रोजेक्ट बनाएँ:**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

## `update`

सब कुछ अपडेट करें: किसी Unity प्रोजेक्ट के MCP प्लगइन और इंस्टॉल किए गए एक्सटेंशन को उनके नवीनतम वर्ज़न पर, फिर स्वयं CLI को।

```bash
unity-mcp-cli update              # the project around the current folder, then the CLI
unity-mcp-cli update ./MyGame      # a project by path
unity-mcp-cli update --check       # only report; exit code 2 when something is outdated
unity-mcp-cli update --no-self     # leave the CLI as it is
unity-mcp-cli update --no-project  # update only the CLI
unity-mcp-cli update --no-extensions
```

प्रोजेक्ट के बाहर यह केवल CLI को अपडेट करता है। `file:` या git से किए गए इंस्टॉल को नहीं छुआ जाता और कुछ भी डाउनग्रेड नहीं किया जाता।

## `install-extension`

Unity-MCP एक्सटेंशन में से किसी एक को — वैकल्पिक टूल पैक जो Animation, Cinemachine, Input System, Navigation, Particle System, ProBuilder, Splines, Terrain, Tilemap और Timeline के लिए MCP टूल्स जोड़ते हैं — किसी Unity प्रोजेक्ट की `Packages/manifest.json` में इंस्टॉल करें। यह *AI Game Developer* एडिटर विंडो के **Extensions** सेक्शन का कमांड-लाइन समकक्ष है, और यह उसी साझा कैटलॉग से इंस्टॉल करता है।

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

| आर्ग्युमेंट / विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[id]` | हाँ (जब तक `--list` न हो) | इंस्टॉल करने के लिए एक्सटेंशन — OpenUPM पैकेज id (`com.ivanmurzak.unity.mcp.tilemap`) या कैटलॉग नाम (`Tilemap`)। केस-असंवेदनशील रूप से मिलाया जाता है। अज्ञात id हर इंस्टॉल करने योग्य एक्सटेंशन की सूची दिखाता है ताकि आप खुद सुधार कर सकें। |
| `[path]` | नहीं | Unity प्रोजेक्ट का पाथ (positional या `--path`)। डिफ़ॉल्ट रूप से वर्तमान डायरेक्टरी, जिसे `install-plugin` की तरह ही सत्यापित किया जाता है। |
| `--extension-version <version>` | नहीं | इंस्टॉल करने के लिए एक्सटेंशन वर्ज़न (डिफ़ॉल्ट रूप से OpenUPM का नवीनतम)। **`--version` नहीं** — वह CLI का अपना ग्लोबल वर्ज़न फ़्लैग है। |
| `--list` | नहीं | इंस्टॉल करने योग्य एक्सटेंशन सूचीबद्ध करें और बाहर निकलें। |

यह कमांड:
1. सभी आवश्यक scopes के साथ **OpenUPM scoped registry** जोड़ती है (`com.ivanmurzak` scope ही एक्सटेंशन को रिज़ॉल्व करने योग्य बनाता है)
2. एक्सटेंशन का पैकेज id `dependencies` में जोड़ती है, इंस्टॉल के समय OpenUPM का `latest` रिज़ॉल्व करते हुए
3. स्वचालित रूप से रिज़ॉल्व किए गए वर्ज़न पर **कभी डाउनग्रेड नहीं करती** — यदि कोई ऊँचा वर्ज़न पहले से इंस्टॉल है, तो उसे बनाए रखा जाता है और एक चेतावनी प्रिंट होती है। किसी विशिष्ट मान को लागू करने के लिए, डाउनग्रेड सहित, `--extension-version` पास करें।
4. **Idempotent** है — दोबारा चलाने पर यदि एक्सटेंशन पहले से अप टू डेट मिलता है, तो यह वैसा ही बताती है और कुछ नहीं लिखती

**उदाहरण — किसी विशिष्ट एक्सटेंशन वर्ज़न को इंस्टॉल करें:**

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

**उदाहरण — इन-प्रोसेस, लाइब्रेरी से:**

```ts
import { installExtension, EXTENSIONS_CATALOG } from 'unity-mcp-cli';

const result = await installExtension({
  unityProjectPath: './MyGame',
  extensionId: 'Tilemap',
});
if (result.kind === 'success') {
  console.log(result.outcome, result.fromVersion, '->', result.toVersion);
}
```

> यह कमांड चलाने के बाद, Unity Editor खोलें (या उस पर फ़ोकस करें) ताकि Package Manager नई डिपेंडेंसी रिज़ॉल्व करे।

## `install-plugin`

Unity-MCP प्लगइन को किसी Unity प्रोजेक्ट की `Packages/manifest.json` में इंस्टॉल करें। वैकल्पिक रूप से उसी रन में RID से मेल खाती MCP सर्वर बाइनरी डाउनलोड करें और टीम एनरोलमेंट कोड रिडीम करें।

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट का पाथ (positional या `--path`)। डिफ़ॉल्ट रूप से वर्तमान डायरेक्टरी; रिज़ॉल्व की गई डायरेक्टरी को एक वास्तविक Unity प्रोजेक्ट (`Packages/manifest.json` मार्कर) के रूप में सत्यापित किया जाता है और, न मिलने पर, एरर ठीक-ठीक बताता है कि क्या जाँचा गया। |
| `--plugin-version <version>` | नहीं | इंस्टॉल करने के लिए प्लगइन वर्ज़न (डिफ़ॉल्ट रूप से [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) का नवीनतम) |
| `--with-server` | नहीं | RID से मेल खाती [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) बाइनरी को भी CLI-प्रबंधित डायरेक्टरी में डाउनलोड करें |
| `--server-version <version>` | नहीं | `--with-server` के साथ डाउनलोड करने के लिए सर्वर वर्ज़न (डिफ़ॉल्ट: CLI का पिन किया गया सर्वर वर्ज़न) |
| `--server-source <path-or-url>` | नहीं | ऑफ़लाइन/CI ओवरराइड — सर्वर को लोकल zip पाथ या URL से इंस्टॉल करें (checksum सत्यापन छोड़ देता है) |
| `--enroll <code>` | नहीं | प्लगइन क्रेडेंशियल के लिए एनरोलमेंट कोड रिडीम करें (साझा मशीन स्टोर में रखा जाता है और इस प्रोजेक्ट पर पिन किया जाता है) |
| `--enroll-stdin` | नहीं | एनरोलमेंट कोड argv के बजाय stdin से पढ़ें (इसे शेल हिस्ट्री से बाहर रखता है) |

यह कमांड:
1. सभी आवश्यक scopes के साथ **OpenUPM scoped registry** जोड़ती है
2. `com.ivanmurzak.unity.mcp` को `dependencies` में जोड़ती है
3. **कभी डाउनग्रेड नहीं करती** — यदि कोई ऊँचा वर्ज़न पहले से इंस्टॉल है, तो उसे बनाए रखा जाता है

**उदाहरण — किसी विशिष्ट प्लगइन वर्ज़न को इंस्टॉल करें:**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

**उदाहरण — टीम एनरोलमेंट कोड रिडीम करें (पाथ डिफ़ॉल्ट रूप से cwd):**

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

> यह कमांड चलाने के बाद, पैकेज इंस्टॉलेशन पूरा करने के लिए प्रोजेक्ट को Unity Editor में खोलें।

## `install-unity`

Unity Hub CLI के ज़रिए कोई Unity Editor वर्ज़न इंस्टॉल करें।

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| आर्ग्युमेंट / विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[version]` | नहीं | इंस्टॉल करने के लिए Unity Editor वर्ज़न (जैसे `6000.3.1f1`) |
| `--path <path>` | नहीं | आवश्यक वर्ज़न किसी मौजूदा प्रोजेक्ट से पढ़ें |

यदि न आर्ग्युमेंट दिया गया है न विकल्प, तो कमांड Unity Hub की रिलीज़ सूची से नवीनतम स्थिर रिलीज़ इंस्टॉल करती है।

**उदाहरण — वह एडिटर वर्ज़न इंस्टॉल करें जिसकी किसी प्रोजेक्ट को ज़रूरत है:**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

## `login`

**ai-game.dev** में साइन इन करें और क्रेडेंशियल को साझा मशीन क्रेडेंशियल स्टोर (`~/.ai-game-dev/credentials.json`) में सहेजें। ब्राउज़र-आधारित **OAuth 2.1 device flow** चलाता है — बनाने या पेस्ट करने के लिए कोई personal access token नहीं है। साइन-इन पूरी मशीन को अधिकृत करता है: यह एक **agent** क्रेडेंशियल बनाता है और उससे एक engine-tools क्रेडेंशियल प्राप्त करता है, और हर उपभोक्ता (CLI की क्लाउड कमांड्स, Unity Editor प्लगइन, डेस्कटॉप ऐप) इसे स्वचालित रूप से रीफ़्रेश करता है — टोकन बैकग्राउंड में रोटेट होते हैं, इसलिए आप प्रति मशीन एक बार साइन इन करते हैं, प्रति सत्र एक बार नहीं।

```bash
unity-mcp-cli login
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `--project <path>` | नहीं | क्रेडेंशियल को साझा मशीन स्टोर के बजाय प्रोजेक्ट-लोकल स्टोर (`<path>/.ai-game-dev/`) में सहेजें |
| `--force` | नहीं | क्रेडेंशियल पहले से मौजूद होने पर भी दोबारा प्रमाणीकरण करें |
| `--tools-only` | नहीं | **केवल engine tools** को अधिकृत करें (कोई agent क्रेडेंशियल सहेजा नहीं जाता, इसलिए डेस्कटॉप-ऐप द्वारा उठाना असंभव है)। CI / ऑटोमेशन रनर के लिए अभिप्रेत, जो फिर अपने स्वयं के रद्द करने योग्य डिवाइस समूह के रूप में दिखते हैं |
| `--yes` | नहीं | प्रॉम्प्ट्स के लिए "yes" मान लें — गैर-इंटरैक्टिव रूप से **खाता बदलने** की पुष्टि के लिए आवश्यक (उस खाते से अलग खाते के रूप में साइन इन करना जो मशीन के पास वर्तमान में है) |

कमांड एक छोटा उपयोगकर्ता कोड और एक सत्यापन URL प्रिंट करती है, आपका ब्राउज़र खोलती है, और तब तक पोल करती है जब तक आप साइन-इन को स्वीकृत नहीं करते। यदि क्रेडेंशियल पहले से मौजूद है तो यह *"Already signed in"* के साथ जल्दी बाहर निकल जाती है — उसे बदलने के लिए `--force` पास करें। मशीन के पास मौजूद खाते से **अलग खाते** के रूप में साइन इन करने पर पहले पुष्टि माँगी जाती है (मना करने पर सब कुछ अपरिवर्तित रहता है); स्क्रिप्ट में पुष्टि के लिए `--yes` पास करें।

**उदाहरण — साइन इन (साझा मशीन स्टोर):**

```bash
unity-mcp-cli login
```

**उदाहरण — CI रनर, केवल engine tools:**

```bash
unity-mcp-cli login --tools-only
```

**उदाहरण — क्रेडेंशियल को किसी विशिष्ट प्रोजेक्ट के बगल में सहेजें:**

```bash
unity-mcp-cli login --project ./MyGame
```

## `unity-account`

वह **Unity खाता** दिखाएँ जिसमें चल रहा Unity Editor साइन इन है, और उसका साइन-इन शुरू करें। Asset Store के
"My Assets" (`assetStore` RQL सोर्स) को इसकी ज़रूरत होती है; Personal लाइसेंस इसके बिना काम करता है, इसलिए एडिटर साइन आउट
होकर भी चल सकता है। यह ai-game.dev खाता नहीं है: वह [`login`](#login) है।

`--sign-in` उस एडिटर में Unity का साइन-इन खोलता है; **उपयोगकर्ता** इसे वहीं, Unity Hub के ज़रिए पूरा करता है। सीधे खोला गया
एडिटर (Hub के ज़रिए नहीं, जैसे किसी पुराने `open` द्वारा) साइन इन नहीं कर सकता: वह केवल "Install Unity Hub" पेश करता है, इसलिए
उत्तर कहता है कि प्रोजेक्ट को [`close`](#close) और [`open`](#open) से फिर से खोलें, जो उसे Hub के ज़रिए शुरू करते हैं। इस तरह खोला गया
एडिटर बिना किसी साइन-इन के वह खाता ले लेता है जिसमें Hub साइन इन है। कोई क्रेडेंशियल CLI से होकर नहीं गुज़रता। कमांड को विंडो वाले एडिटर में खुला प्रोजेक्ट
चाहिए ([`open`](#open)) और यह कभी हेडलेस एडिटर शुरू नहीं करती, जो साइन-इन नहीं दिखा सकता। एजेंट यही क्वेरी
`rql` से चला सकते हैं: `assetStore | select signedIn userName` और `assetStore | call SignIn()`।

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`; डिफ़ॉल्ट: वर्तमान डायरेक्टरी) |
| `--sign-in` | नहीं | एडिटर साइन इन न होने पर Unity का साइन-इन खोलें (साइन इन होने पर: access token रीफ़्रेश करता है) |
| `--format <format>` | नहीं | `text`, `json` या `ndjson` |

एडिटर के उत्तर के साथ `0` पर, और कोई एडिटर उत्तर न दे तो `7` पर बाहर निकलती है। इस कमांड के लिए बहुत पुराने प्लगइन को
[`update`](#update) चलाने का संकेत मिलता है।

## `open`

किसी Unity प्रोजेक्ट को Unity Editor में खोलें। डिफ़ॉल्ट रूप से, यदि कनेक्शन विकल्प दिए गए हों, तो MCP कनेक्शन एनवायरनमेंट वेरिएबल सेट करता है। MCP कनेक्शन के बिना खोलने के लिए `--no-connect` का उपयोग करें।

जब Unity Hub इंस्टॉल होता है, तो एडिटर उसी के ज़रिए वैसे ही खोला जाता है जैसे Hub स्वयं करता है (`-useHub -hubIPC`, कोई क्रेडेंशियल नहीं:
एडिटर Hub से उसका सत्र माँगता है, और Hub न चल रहा हो तो उसे बैकग्राउंड में शुरू करता है)। फिर एडिटर
वह Unity खाता ले लेता है जिसमें Hub साइन इन है, जिसकी Asset Store के "My Assets" को ज़रूरत होती है (देखें
[`unity-account`](#unity-account))। हेडलेस (`--headless`) एडिटर पहले की तरह खोला जाता है।

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

| विकल्प | Env वेरिएबल | आवश्यक | विवरण |
|---|---|---|---|
| `[path]` | — | नहीं | Unity प्रोजेक्ट का पाथ (positional या `--path`)। डिफ़ॉल्ट रूप से वर्तमान कार्यशील डायरेक्टरी। |
| `--unity <version>` | — | नहीं | उपयोग करने के लिए विशिष्ट Unity Editor वर्ज़न (डिफ़ॉल्ट रूप से प्रोजेक्ट सेटिंग्स का वर्ज़न, अन्यथा सबसे ऊँचा इंस्टॉल किया गया) |
| `--editor-path <path>` | — | नहीं | Unity Editor executable का स्पष्ट पाथ। Unity Hub खोज को छोड़ देता है, कस्टम इंस्टॉल स्थानों के लिए उपयोगी। |
| `--no-connect` | — | नहीं | MCP कनेक्शन एनवायरनमेंट वेरिएबल के बिना खोलें |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | नहीं | कनेक्ट करने के लिए MCP सर्वर URL (CLI अभी भी पुराना उपनाम `UNITY_MCP_HOST` एक्सपोर्ट करता है, जिसे प्लगइन भी स्वीकार करता है) |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | नहीं | कनेक्शन को ज़बरदस्ती चालू रखें |
| `--token <token>` | `UNITY_MCP_TOKEN` | नहीं | प्रमाणीकरण टोकन |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | नहीं | प्रमाणीकरण मोड: `none`, `oauth`, या `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | नहीं | सक्षम करने के लिए टूल्स की अल्पविराम से अलग सूची |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | नहीं | ट्रांसपोर्ट मेथड: `streamableHttp` या `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | नहीं | MCP सर्वर ऑटो-स्टार्ट नियंत्रित करने के लिए `true` या `false` पर सेट करें |
| `--headless` | — | नहीं | एडिटर को विंडो के बिना शुरू करें (`-batchmode`; ग्राफ़िक्स चालू रहते हैं, इसलिए कैमरे फिर भी रेंडर करते हैं) और तब तक प्रतीक्षा करें जब तक उसका MCP सर्वर उत्तर न दे। `--keep-connected` निहित है। प्रोजेक्ट का चल रहा एडिटर फिर से उपयोग होता है; यहाँ शुरू किया गया एडिटर CLI का अपना होता है (देखें [`close --owned-only`](#close)), और समानांतर कॉल्स केवल एक ही शुरू करती हैं। |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS` (headless) / `UNITY_MCP_IDLE_TIMEOUT` (window) | नहीं | बिना किसी टूल कॉल के इतने सेकंड बाद एडिटर बंद करें (पहले बदले गए सीन और assets सहेजता है) |
| `--ready-timeout <ms>` | — | नहीं | `--headless` के साथ: एडिटर के तैयार होने की कितनी देर प्रतीक्षा करें (डिफ़ॉल्ट: `300000`) |
| `--no-auto-dismiss-launch-errors` | — | नहीं | Unity Editor के "compile errors at launch" डायलॉग का स्वतः-खारिज होना अक्षम करें (डिफ़ॉल्ट: सक्षम) |
| `--launch-dismiss-timeout-ms <ms>` | — | नहीं | launch-errors स्वतः-खारिज पोलिंग लूप के लिए कुल टाइमआउट (मिलीसेकंड) (डिफ़ॉल्ट: `30000`) |
| `--launch-dismiss-poll-interval-ms <ms>` | — | नहीं | launch-errors स्वतः-खारिज लूप के लिए पोलिंग अंतराल (मिलीसेकंड) (डिफ़ॉल्ट: `1500`) |

`--headless` के बिना, प्रोजेक्ट के लिए CLI द्वारा शुरू किए गए हेडलेस एडिटर (`exec`, `open --headless`) से पहले सहेजने और बंद होने को कहा जाता है, ताकि विंडो खुल सके।

एडिटर प्रोसेस detached मोड में spawn होता है। डिफ़ॉल्ट रूप से, एडिटर spawn करने के बाद, `open` Unity के "compile errors at launch" डायलॉग (Unity 2020.2+ पर `"Enter Safe Mode?"`, पुरानी रिलीज़ पर `"Hold On" / "Compiler Errors"`) के लिए पोल करता है और `Ignore` पर क्लिक करता है ताकि एडिटर इनिशियलाइज़ होना पूरा करे — इसके बिना, कोई भी इन-Editor ऑटोमेशन जिसे उस स्थिति के बाद चलना है जहाँ Unity स्वयं कंपाइल नहीं कर सकता (जैसे NuGet डिपेंडेंसी रिज़ॉल्वर), खुद को ठीक नहीं कर सकता। डायलॉग तब सामने आता है जब Unity बूट हो चुका हो, Package Manager से कनेक्ट हो चुका हो, और कंपाइल करना शुरू कर चुका हो — अनुभव के अनुसार तेज़ मशीन पर ~6s और धीमी मशीन पर अधिक — इसलिए पोलिंग लूप में एक grace window होती है जिसके बाद, यदि कोई डायलॉग नहीं दिखा, तो वह जल्दी बाहर निकल जाता है। grace window को Unity के पूरे स्टार्टअप चरण को कवर करना होता है वरना लूप डायलॉग दिखने से पहले ही बाहर निकल जाता है (issue #737); डायलॉग न होने की स्थिति में यह कभी पूरा `--launch-dismiss-timeout-ms` नहीं चलता। यदि डायलॉग दिखता है (और सफलतापूर्वक खारिज हो जाता है), तो पोलिंग कुल टाइमआउट तक जारी रहती है ताकि फिर से दिखने वाला डायलॉग (रिज़ॉल्वर एक एरर ठीक करता है → अगले एरर के साथ डायलॉग फिर दिखता है) फिर से खारिज हो जाए। लाइब्रेरी-मोड कॉलर एक `AbortSignal` (`OpenProjectOptions` पर `launchDismissAbortSignal`) दे सकते हैं ताकि उनका अपना readiness सिग्नल आते ही लूप रुक जाए।

### स्वतः-खारिज के लिए प्लेटफ़ॉर्म आवश्यकताएँ

| प्लेटफ़ॉर्म | आवश्यकता | टिप्पणियाँ |
|---|---|---|
| **Windows** | बिल्ट-इन (Win32 API) | PowerShell से संचालित `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)` का उपयोग करता है। किसी अतिरिक्त सेटअप की ज़रूरत नहीं। |
| **macOS** | टर्मिनल (या `unity-mcp-cli` बाइनरी) को **Accessibility अनुमति** दी जानी चाहिए। System Settings → Privacy & Security → Accessibility। | AppleScript / `osascript` के ज़रिए लागू। इस अनुमति के बिना, `osascript` हर पोल टिक पर एरर रिपोर्ट करता है और डायलॉग खारिज नहीं हो सकता। |
| **Linux/X11** | `PATH` पर `xdotool` (जैसे `sudo apt-get install xdotool`)। | पहले संस्करण में Wayland **समर्थित नहीं** है — Wayland समर्थन के लिए upstream issues पर नज़र रखें। |

पूरी तरह ऑप्ट आउट करने के लिए `--no-auto-dismiss-launch-errors` पास करें।

**उदाहरण — MCP कनेक्शन के साथ खोलें:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**उदाहरण — MCP कनेक्शन के बिना खोलें (सरल open):**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**उदाहरण — प्रमाणीकरण और विशिष्ट टूल्स के साथ खोलें:**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth token \
  --tools rql-query,rql-mutate,rql-schema
```

## `close`

किसी दिए गए प्रोजेक्ट पाथ के लिए चल रहे Unity Editor इंस्टेंस को शालीनता से बंद करें। [`open`](#open) का सममित समकक्ष — स्क्रिप्टेड वर्कफ़्लो (CI एजेंट, पाइपलाइन एक्ज़िक्यूटर, इंटीग्रेशन टेस्ट फ़िक्स्चर) के लिए जिन्हें OS-स्तर पर प्रोसेस kill किए बिना साफ़ tear-down चाहिए।

```bash
unity-mcp-cli close ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट का पाथ (positional, डिफ़ॉल्ट रूप से वर्तमान डायरेक्टरी) |
| `--timeout <seconds>` | नहीं | शालीन-बंद टाइमआउट सेकंड में (डिफ़ॉल्ट: 30) |
| `--force` | नहीं | यदि Editor `--timeout` के भीतर बंद नहीं होता, तो उसे ज़बरदस्ती kill करें |
| `--owned-only` | नहीं | केवल वह Editor बंद करें जिसे CLI ने शुरू किया (`exec`, `open --headless`); उपयोगकर्ता द्वारा खोले गए Editor को कभी नहीं छुआ जाता |

**यह कैसे काम करता है:**

0. CLI द्वारा शुरू किया गया Editor प्रोजेक्ट की session lease (`.ai-game-dev/session.json`: PID, प्रोसेस प्रारंभ समय, nonce) में दर्ज होता है। यदि lease किसी जीवित Editor का नाम देती है, तो `close` उसके प्लगइन से सहेजने और बंद होने को कहता है (`editor-session-quit` सिस्टम टूल, lease nonce से जाँचा गया, उस सर्वर URL पर जो lease ने Editor शुरू होने पर दर्ज किया था) — यह हेडलेस Editor तक भी पहुँचता है, जहाँ `WM_CLOSE` नहीं पहुँच सकता — और नीचे के चरण छोड़ देता है। `--owned-only` के साथ यह यहीं रुक जाता है: कोई lease नहीं, तो कुछ भी बंद नहीं होता। यदि `--timeout` के बाद (बिना `--force` के) वह Editor अभी भी चल रहा है और कोई मोडल डायलॉग खुला है, तो `close` 18 पर बाहर निकलता है और उसका नाम बताता है, जैसे चरण 3 में।
1. `<project>/Temp/UnityLockfile` (4-बाइट little-endian uint32) पढ़कर चल रहे Editor का PID रिज़ॉल्व करता है और पुरानी lock फ़ाइलों को संभालने के लिए प्रोसेस गणना से क्रॉस-चेक करता है।
2. एक शालीन-बंद सिग्नल भेजता है — Linux/macOS पर `SIGTERM`, Windows पर `taskkill` (`/F` के बिना) — जिससे Unity autosave / asset-import पूरा कर सके।
3. हर 250ms पर पोल करता है जब तक प्रोसेस बंद न हो जाए या `--timeout` बीत न जाए। इस बीच यह Editor के किसी मोडल डायलॉग को खोजता है (Windows, macOS, Linux): बिना सहेजे बदलावों के साथ शालीन बंद "Scene(s) Have Been Modified" [Save | Don't Save | Cancel] खोलता है, और फिर Editor किसी व्यक्ति की प्रतीक्षा करता है। `--force` के बिना, जैसे ही लगातार दो स्कैन डायलॉग देखते हैं (बंद करने के लगभग 3 s बाद), `close` एग्ज़िट कोड 18 के साथ रुक जाता है और डायलॉग तथा उसके बटनों के नाम बताता है (इसका उत्तर Editor में या [`dialog --press`](#dialog) से दें); `--force` के साथ यह hard kill से पहले फिर भी `--timeout` तक प्रतीक्षा करता है।
4. यदि टाइमआउट समाप्त हो जाए AND `--force` सेट हो, तो `SIGKILL` / `taskkill /F` पर लौटता है।
5. Idempotent — पहले से बंद Editor (या ऐसा प्रोजेक्ट जिसका Editor कभी चल ही नहीं रहा था) को बंद करना `no running Editor for project at <path>` के साथ 0 पर बाहर निकलता है।
6. ऐसे किसी भी पाथ पर कार्य करने से इनकार करता है जो Unity प्रोजेक्ट रूट नहीं है (`ProjectSettings/ProjectVersion.txt` मौजूद होना चाहिए) — होस्ट पर सभी Unity को गलती से kill करने वाले आह्वानों से बचाता है।

> **Windows हेडलेस चेतावनी:** शालीन-बंद चरण `taskkill` (`/F` के बिना) का उपयोग करता है, जो `WM_CLOSE` भेजता है। वह संदेश केवल उन प्रोसेस तक पहुँचता है जिनकी top-level विंडो CLI वाले **उसी desktop/session** पर हो। यदि Unity को session 0 में किसी Windows सेवा (या किसी अन्य गैर-इंटरैक्टिव डेस्कटॉप) ने लॉन्च किया था, तो शालीन-बंद चुपचाप गिर जाएगा, `--timeout` बीत जाएगा, और `--force` ही Editor को बंद करने का एकमात्र रास्ता बचेगा। हेडलेस CI रनर में इसके अनुसार योजना बनाएँ। CLI द्वारा शुरू किया गया Editor (चरण 0) प्रभावित नहीं होता: वह अपने प्लगइन के ज़रिए बंद होता है।

**उदाहरण — बंद करें, 60s के बाद force पर लौटें:**

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

**उदाहरण — ऑटोमेशन स्क्रिप्ट के अंत में साफ़ tear-down:**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

## `tools`

प्रोजेक्ट का Unity Editor जो AI टूल्स पेश करता है उनकी सूची दें — सीन, assets, टेस्ट, स्क्रीनशॉट और वह सब कुछ जो इंस्टॉल किए गए
एक्सटेंशन जोड़ते हैं — हर टूल क्या करता है और उसके इनपुट सहित, या किसी एक टूल का विस्तृत विवरण दिखाएँ (पूरा विवरण, इनपुट, एक उदाहरण
कॉल, उसका JSON input schema)। कोई एडिटर न चल रहा हो तो [`exec`](#exec) की तरह एक हेडलेस एडिटर शुरू करता है। सूची MCP पर पढ़ी जाती है
(`tools/list`) और प्रोजेक्ट में सहेजी जाती है (git-ignored), इसलिए एडिटर बंद होने पर भी `--cached` तुरंत उत्तर देता है।
छिपे हुए टूल्स (जो MCP क्लाइंट्स को पेश नहीं किए जाते) नाम से सूचीबद्ध होते हैं; `run-tool` उन्हें फिर भी कॉल करता है।

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[name]` | नहीं | एक टूल नाम (विवरण) या एक regex (फ़िल्टर) |
| `--path <path>` | नहीं | Unity प्रोजेक्ट पाथ (डिफ़ॉल्ट: वर्तमान डायरेक्टरी) |
| `--cached` | नहीं | एडिटर से संपर्क किए बिना सहेजी गई सूची से उत्तर दें |
| `--no-start` | नहीं | कभी एडिटर शुरू न करें; कोई उत्तर न दे तो सहेजी गई सूची का उपयोग करें |
| `--once` | नहीं | यदि इस कॉल ने एडिटर शुरू किया था, तो बाद में उसे बंद करें |
| `--ready-timeout <ms>` | नहीं | शुरू किए गए एडिटर की प्रतीक्षा करें (डिफ़ॉल्ट: 300000) |
| `--url <url>` / `--token <token>` | नहीं | सर्वर ओवरराइड |
| `--format <format>` | नहीं | `text`, `json` या `ndjson` |

## `rql`

कमांड लाइन से RQL, बिना JSON quoting के: एक क्वेरी, या `;` से अलग की गई कई क्वेरी। जिस क्वेरी का अंतिम चरण `set` या `call` है वह
`rql-mutate` पर जाती है (कई होने पर: जब उनमें से कोई भी ऐसी हो, तो हर एक को ऐसा होना चाहिए; हर एक अपना अलग सब-या-कुछ-नहीं बदलाव है, और जो पहली विफल होती है वह बाकी को रोक देती है), बाकी कोई भी `rql-query` पर; `--schema [type]` `rql-schema` चलाता है। `run-tool` की तरह हेडलेस एडिटर शुरू करता है।
Unity बदलाव को तुरंत डिस्क पर सहेजता है, इसलिए किसी `set` / `call` के बाद CLI उसके द्वारा लिखी गई सीन और asset फ़ाइलें प्रिंट करता है
(`Saved to disk: Assets/Scenes/Forest.unity`)। किसी दूसरी भाषा की वर्तनी वाले ऑपरेटर (`startsWith`, `like`) को
RQL की वर्तनी के साथ एक संकेत मिलता है। `rql --help` रेसिपी सूचीबद्ध करता है।

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[query]` | हाँ, जब तक `--schema` / `-f` न हो | RQL क्वेरी (इसे single-quote करें; अंदर की strings double quotes लेती हैं) |
| `-f, --file <file>` | नहीं | क्वेरी फ़ाइल से पढ़ें (`-`: stdin), ऐसे शेल के लिए जो अंदर के quotes हटा देता है |
| `--schema [type]` | नहीं | RQL सोर्स, या किसी एक टाइप के मेंबर्स सूचीबद्ध करें |
| `--dry-run` | नहीं | `set` / `call` के लिए: दिखाएँ कि क्या बदलेगा, कुछ न बदलें |
| `--max-targets <n>` | नहीं | `set` / `call` के लिए: 200 से अधिक टार्गेट (1000 तक) |
| `--path <path>` | नहीं | Unity प्रोजेक्ट पाथ (डिफ़ॉल्ट: वर्तमान डायरेक्टरी) |
| `--no-start` / `--once` | नहीं | `run-tool` की तरह (`--url`, `--token`, `--raw`, `--timeout`, `--ready-timeout` भी काम करते हैं) |
| `--format <format>` | नहीं | `text`, `json` या `ndjson` |

## `run-tool`

HTTP API के ज़रिए सीधे कोई MCP टूल एक्ज़िक्यूट करें। सर्वर URL और प्रमाणीकरण टोकन वर्तमान कनेक्शन मोड (Custom या Cloud) के आधार पर प्रोजेक्ट की कॉन्फ़िग फ़ाइल (`UserSettings/AI-Game-Developer-Config.json`) से **स्वचालित रूप से रिज़ॉल्व** होते हैं।

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `<tool-name>` | हाँ | एक्ज़िक्यूट करने के लिए MCP टूल का नाम |
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`) — कॉन्फ़िग पढ़ने और पोर्ट का पता लगाने के लिए उपयोग होता है |
| `--url <url>` | नहीं | सीधा सर्वर URL ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--token <token>` | नहीं | Bearer token ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--input <json>` | नहीं | टूल आर्ग्युमेंट की JSON string (डिफ़ॉल्ट `{}`) |
| `--input-file <file>` | नहीं | JSON आर्ग्युमेंट फ़ाइल से पढ़ें |
| `--raw` | नहीं | कच्चा JSON आउटपुट (कोई फ़ॉर्मैटिंग नहीं, कोई spinner नहीं) |
| `--format <format>` | नहीं | `text` (डिफ़ॉल्ट), `json` या `ndjson` — देखें [मशीन-पठनीय आउटपुट](#exit-codes-and-machine-readable-output) |
| `--timeout <ms>` | नहीं | अनुरोध टाइमआउट मिलीसेकंड में (डिफ़ॉल्ट: 60000) |
| `--no-start` | नहीं | कभी एडिटर शुरू न करें; कोई उत्तर न दे तो विफल हों (exit 7) |
| `--once` | नहीं | यदि इस कॉल ने एडिटर शुरू किया था, तो कॉल के बाद उसे बंद करें (एक-बार का CI उपयोग) |
| `--ready-timeout <ms>` | नहीं | शुरू किए गए एडिटर के तैयार होने की कितनी देर प्रतीक्षा करें (डिफ़ॉल्ट: 300000) |

**एडिटर शुरू करना।** जब प्रोजेक्ट का कोई एडिटर उत्तर नहीं देता, तो `run-tool` ठीक
[`exec`](#exec) की तरह एक हेडलेस एडिटर शुरू करता है (साझा session lease, कभी ऐसा एडिटर बंद नहीं करता जिसे उसने शुरू नहीं किया) — लेकिन केवल ऐसे प्रोजेक्ट के लिए जो
`ProjectSettings/ProjectVersion.txt` में अपना Unity वर्ज़न बताता है, ताकि यह कभी एडिटर का अनुमान न लगाए। `--no-start` इसे बंद करता है;
`--once` कॉल को एक-बार का बनाता है: इस कॉल द्वारा शुरू किया गया एडिटर कॉल के बाद फिर से बंद कर दिया जाता है (और तब भी जब वह कभी
तैयार नहीं हुआ)। `run-system-tool` भी ऐसा ही व्यवहार करता है।

एरर के साथ उत्तर देने वाला टूल 6 पर बाहर निकलता है; अज्ञात टूल 2 (`NOT_FOUND`), टाइमआउट 14, पहुँच से बाहर एडिटर 7, व्यस्त
एडिटर 17, और मोडल डायलॉग द्वारा अवरुद्ध कॉल 18 — HTTP स्टेटस सहित पूरी तालिका
[एग्ज़िट कोड](#exit-codes-and-machine-readable-output) में है।

**URL रिज़ॉल्यूशन प्राथमिकता:**
1. `--url` → सीधे उपयोग करें
2. कॉन्फ़िग फ़ाइल → `host` (Custom मोड) या हार्डकोडेड क्लाउड एंडपॉइंट (Cloud मोड)
3. प्रोजेक्ट पाथ से निर्धारित (deterministic) पोर्ट

**प्रमाणीकरण** प्रोजेक्ट कॉन्फ़िग से स्वचालित रूप से पढ़ा जाता है (Custom मोड में `token`, Cloud मोड में `cloudToken`)। कॉन्फ़िग से प्राप्त टोकन को स्पष्ट रूप से ओवरराइड करने के लिए `--token` का उपयोग करें।

**उदाहरण — कोई टूल कॉल करें (URL और प्रमाणीकरण कॉन्फ़िग से):**

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

**उदाहरण — स्पष्ट URL ओवरराइड:**

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

**उदाहरण — कच्चा JSON आउटपुट पाइप करें:**

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

**उदाहरण — CI में एक-बार की कॉल (हेडलेस एडिटर शुरू करती है, बाद में उसे बंद करती है):**

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

## `exec`

Unity Editor में C# चलाएँ और परिणाम प्रिंट करें। यदि प्रोजेक्ट पहले से खुला है (विंडो के साथ या हेडलेस) तो `exec` उसी एडिटर का उपयोग करता है और उसे कभी बंद नहीं करता। अन्यथा यह एक हेडलेस एडिटर शुरू करता है जो अगली कॉल्स के लिए गर्म रहता है और टूल कॉल के बिना `--idle` सेकंड के बाद अपने आप बंद हो जाता है (बदले गए सीन और assets सहेजते हुए)। जिस एडिटर को यह शुरू करता है वह प्रोजेक्ट की session lease (`.ai-game-dev/session.json`) में दर्ज होता है और एक file lock के अंतर्गत शुरू होता है, इसलिए समानांतर `exec` कॉल्स एक ही एडिटर साझा करती हैं; `close --owned-only` उसे बंद करता है, और `open` (विंडो के साथ) विंडो खोलने से पहले उसे बंद करता है। ये session फ़ाइलें मशीन-लोकल हैं: `.ai-game-dev/.gitignore` उन्हें git से बाहर रखता है। यदि प्रोजेक्ट किसी ऐसे एडिटर में खुला है जिसे CLI ने शुरू नहीं किया और उसका प्लगइन 60 s के भीतर उत्तर नहीं देता (डिस्कनेक्ट है, या ai-game.dev क्लाउड से कनेक्ट है), तो `exec` प्रतीक्षा करने के बजाय एक संकेत के साथ रुक जाता है: प्लगइन कनेक्ट करें, उसके कनेक्शन को Custom (एक लोकल सर्वर) पर बदलें, या `--url` पास करें।

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[code]` | नहीं | C# स्निपेट: सादे स्टेटमेंट, अंतिम expression परिणाम है। वैकल्पिक रूप से `-f` या stdin। |
| `--path <path>` | नहीं | Unity प्रोजेक्ट पाथ (डिफ़ॉल्ट: वर्तमान डायरेक्टरी) |
| `-f, --file <file>` | नहीं | कोड फ़ाइल से पढ़ें (stdin के लिए `-`) |
| `--using <namespace>` | नहीं | अतिरिक्त namespace, दोहराने योग्य |
| `--include <path>` | नहीं | सहायक `.cs` फ़ाइल या फ़ोल्डर (रिकर्सिव), `<Project>/.ai-game-dev/snippets` के सापेक्ष, दोहराने योग्य |
| `--window` | नहीं | एडिटर को हेडलेस के बजाय विंडो के साथ शुरू करें (केवल तब जब वह अभी तक नहीं चल रहा) |
| `--idle <seconds>` | नहीं | `exec` द्वारा शुरू किए गए एडिटर को इतने निष्क्रिय सेकंड के बाद बंद करें (डिफ़ॉल्ट: 300) |
| `--ready-timeout <ms>` | नहीं | एडिटर के तैयार होने की कितनी देर प्रतीक्षा करें (डिफ़ॉल्ट: 300000) |
| `--timeout <ms>` | नहीं | टूल कॉल टाइमआउट (डिफ़ॉल्ट: 310000) |
| `--raw` | नहीं | कच्चा टूल response JSON प्रिंट करें |
| `--format <format>` | नहीं | `text` (डिफ़ॉल्ट), `json` या `ndjson` |
| `--once` | नहीं | यदि इस कॉल ने एडिटर शुरू किया था, तो कॉल के बाद उसे बंद करें |
| `--url <url>` | नहीं | सर्वर URL ओवरराइड; लोकल URL नए शुरू किए गए एडिटर से वह सर्वर भी शुरू करवाता है |
| `--token <token>` | नहीं | Bearer token ओवरराइड |

`exec` `script-execute` टूल को कॉल करता है, इसलिए snippets, `usings` और `include` के लिए Code-Exec समर्थन वाला प्लगइन वर्ज़न चाहिए। कंपाइल एरर या एक्सेप्शन कोड 6 के साथ बाहर निकलता है और आपकी अपनी पंक्तियों की ओर इशारा करता है।

> **साइन इन नहीं हैं?** नया प्रोजेक्ट **Cloud** कनेक्शन मोड में शुरू होता है। ai-game.dev साइन-इन के बिना, `exec`,
> `run-tool`, `compile` और `tools` इसके बजाय प्रोजेक्ट के **लोकल** MCP सर्वर का उपयोग करते हैं (वे stderr पर ऐसा बताते हैं): जिस एडिटर को
> वे शुरू करते हैं उसे मेमोरी में इसे सर्व करने को कहा जाता है — प्रोजेक्ट कॉन्फ़िग नहीं बदलता। क्लाउड के ज़रिए काम करने के लिए `login` चलाएँ।
>
> CLI द्वारा शुरू किया गया हेडलेस एडिटर तब भी चलता रहता है जब प्रोजेक्ट की स्क्रिप्ट्स कंपाइल नहीं होतीं
> (`-ignorecompilererrors`), ताकि `compile` एरर रिपोर्ट कर सके; उसका लॉग `Logs/unity-mcp-cli-editor.log` है।

## `test`

प्रोजेक्ट के Unity टेस्ट वैसे ही चलाएँ जैसे CI करता है: **Unity के अपने `-runTests` के साथ एक batch-mode एडिटर** — कोई AI Game
Developer प्लगइन और कोई MCP सर्वर शामिल नहीं होता, इसलिए यह ऐसे प्रोजेक्ट पर भी काम करता है जिसका प्लगइन टूटा हुआ या गायब है। निर्णय
रन द्वारा लिखी गई NUnit रिपोर्ट से आता है (उसी पाथ की पुरानी रिपोर्ट पहले हटा दी जाती है), कभी केवल
एडिटर के एग्ज़िट कोड से नहीं।

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`, डिफ़ॉल्ट: वर्तमान डायरेक्टरी) |
| `--mode <mode>` | नहीं | `EditMode` (डिफ़ॉल्ट) या `PlayMode` |
| `--filter <pattern>` | नहीं | Unity `-testFilter`: पूरे टेस्ट नाम या एक regex, `;` कई को अलग करता है |
| `--category <names>` | नहीं | Unity `-testCategory` |
| `--assembly <names>` | नहीं | Unity `-assemblyNames` |
| `--output <file>` | नहीं | NUnit रिपोर्ट पाथ (डिफ़ॉल्ट: `<project>/Logs/test-results.xml`) |
| `--junit-output <file>` | नहीं | एक JUnit रिपोर्ट भी लिखें |
| `--log-file <file>` | नहीं | एडिटर लॉग (डिफ़ॉल्ट: `.log` के साथ रिपोर्ट पाथ) |
| `--timeout <seconds>` | नहीं | इतने समय के बाद एडिटर (और उसकी चाइल्ड प्रोसेस) को kill करें (डिफ़ॉल्ट: 3600) |
| `--nographics` | नहीं | GPU के बिना चलाएँ |
| `--unity <version>` / `--editor-path <path>` | नहीं | उपयोग करने के लिए एडिटर (डिफ़ॉल्ट: प्रोजेक्ट का वर्ज़न) |
| `--format <format>` | नहीं | `text`, `json`, `ndjson` या `github` |
| `-- <args>` | नहीं | अतिरिक्त एडिटर आर्ग्युमेंट; जिन्हें `test` प्रबंधित करता है (`-batchmode`, `-runTests`, `-quit`, ...) वे अस्वीकार किए जाते हैं |

**एग्ज़िट कोड:** `0` सभी पास (शून्य टेस्ट वाला रन चेतावनी के साथ पास होता है), `8` टेस्ट विफल, `6` कोई निर्णय नहीं (कंपाइल
एरर — `file(line,col): error CS...` के रूप में सूचीबद्ध — लाइसेंस समस्या, क्रैश, या प्रोजेक्ट **पहले से किसी
एडिटर में खुला है**: दूसरा एडिटर इसे नहीं खोल सकता, इसलिए उस एडिटर पर `run-tool tests-run` का उपयोग करें), `14` टाइमआउट, `2` गलत
आर्ग्युमेंट। Ctrl+C या रद्द किया गया CI जॉब भी एडिटर को रोक देता है।

## `compile`

प्रोजेक्ट के एडिटर के विरुद्ध "क्या मेरा C# कंपाइल होता है?": assets रीफ़्रेश करता है ताकि बदली गई स्क्रिप्ट्स कंपाइल हों, कंपाइल
और उसके domain reload की प्रतीक्षा करता है, और एडिटर के Console से कंपाइलर एरर और चेतावनियाँ फ़ाइल, पंक्ति और
कॉलम के साथ रिपोर्ट करता है। कोई एडिटर न चल रहा हो तो एक हेडलेस एडिटर शुरू करता है (`exec` की तरह अगली कॉल के लिए गर्म रखा जाता है); गर्म
एडिटर के विरुद्ध कॉल में कुछ सेकंड लगते हैं।

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`) |
| `--strict` | नहीं | कंपाइलर चेतावनियों पर भी विफल हों |
| `--timeout <seconds>` | नहीं | कंपाइल और domain reload की प्रतीक्षा करें (डिफ़ॉल्ट: 180) |
| `--ready-timeout <ms>` | नहीं | शुरू किए गए एडिटर की प्रतीक्षा करें (डिफ़ॉल्ट: 300000) |
| `--once` | नहीं | यदि इस कॉल ने एडिटर शुरू किया था, तो बाद में उसे बंद करें |
| `--url <url>` / `--token <token>` | नहीं | सर्वर ओवरराइड, जैसे `exec` के लिए |
| `--format <format>` | नहीं | `text`, `json`, `ndjson` या `github` |

**एग्ज़िट कोड:** `0` कंपाइल होता है, `6` कंपाइल एरर (या `--strict` के साथ चेतावनियाँ), `7` किसी एडिटर तक नहीं पहुँचा जा सका, `14`
टाइमआउट, `18` कोई मोडल डायलॉग एडिटर को अवरुद्ध करता है। डायग्नोस्टिक्स कंपाइलर के अपने
`file(line,col): error CS0103: ...` रूप में stderr पर जाते हैं।

## `dialog`

एक मोडल डायलॉग (सेव प्रॉम्प्ट, "Enter Safe Mode?", `EditorUtility.DisplayDialog`) एडिटर के मुख्य थ्रेड को अवरुद्ध करता है, इसलिए
हर टूल कॉल तब तक प्रतीक्षा करती है जब तक कोई उसका उत्तर न दे। `dialog` प्रोजेक्ट के एडिटर के डायलॉग सूचीबद्ध करता है और कोई
बटन दबा सकता है। `status`, `run-tool`, `exec`, `compile` और `wait-for-ready` ऐसे डायलॉग को सादे टाइमआउट के बजाय एग्ज़िट कोड `18`
(`AWAITING_INPUT`) के रूप में रिपोर्ट करते हैं। Windows, macOS और Linux; हेडलेस (`-batchmode`) एडिटर कभी
डायलॉग नहीं दिखाता।

| प्लेटफ़ॉर्म | कैसे | आवश्यकता |
|---|---|---|
| **Windows** | PowerShell के ज़रिए Win32 (`EnumWindows`, `BM_CLICK`) | कोई नहीं |
| **macOS** | `osascript -l JavaScript` के ज़रिए System Events UI स्क्रिप्टिंग: एडिटर की डायलॉग विंडो और sheets, उनके टेक्स्ट और बटन | CLI चलाने वाले ऐप (आपका टर्मिनल या AI एजेंट ऐप) के लिए **Accessibility अनुमति**: System Settings → Privacy & Security → Accessibility। इसके बिना `dialog` उसी संकेत के साथ विफल होता है, और exit-18 जाँच को कोई डायलॉग नहीं मिलता। |
| **Linux** | एक `python3` सहायक (PyGObject) के ज़रिए AT-SPI accessibility bus: एडिटर की डायलॉग और alert विंडो (GTK, जिसका उपयोग Unity का `EditorUtility.DisplayDialog` करता है), उनके labels और push buttons, जिन्हें उनके `click` action से दबाया जाता है। X11 और Wayland पर काम करता है | PyGObject और AT-SPI typelib के साथ `python3` (Debian/Ubuntu: `python3-gi gir1.2-atspi-2.0`; Fedora: `python3-gobject at-spi2-core`; Arch: `python-gobject at-spi2-core`) और एक चल रहा accessibility bus (`at-spi2-core`, GNOME, KDE और अधिकांश डेस्कटॉप में माँग पर शुरू होता है)। CLI को उसी डेस्कटॉप सत्र में चलाएँ जो एडिटर चलाता है। गायब पैकेज, पहुँच से बाहर bus या कोई ग्राफ़िकल सत्र न होने पर `dialog` समाधान बताने वाले संकेत के साथ विफल होता है, और exit-18 जाँच को कोई डायलॉग नहीं मिलता। |

Windows Unity की अपनी मोडल विंडो भी सूचीबद्ध करता है, जैसे "Missing signature" जिसे Unity 6.6 किसी
unsigned `.unitypackage` (हर Asset Store पैकेज) को इम्पोर्ट करने से पहले दिखाता है: Unity उनका टेक्स्ट और बटन स्वयं बनाता है, इसलिए सूची केवल
उनका शीर्षक और एक बटन, **Cancel**, दिखाती है, जिसे `--press Cancel` Escape कुंजी के रूप में भेजता है। कोई भी अन्य उत्तर (Import Anyway)
उपयोगकर्ता का है, एडिटर में।

डायलॉग id सूची से आता है और जब तक वह डायलॉग खुला है तब तक वही रहता है (Windows: उसका window handle; macOS और
Linux: उसके pid, शीर्षक, टेक्स्ट और बटनों का hash)। बटन label कभी स्क्रिप्ट में नहीं डाला जाता, केवल डेटा के रूप में पास किया जाता है।

हेडलेस Linux मशीन पर, एडिटर (`-batchmode` के बिना) और CLI को एक ही
`dbus-run-session` के अंदर `Xvfb` के तहत चलाएँ, `at-spi2-core` इंस्टॉल करके। यदि उसी उपयोगकर्ता का कोई डेस्कटॉप सत्र भी है, तो उस निजी
सत्र को उसका अपना `XDG_RUNTIME_DIR` दें: अन्यथा उसका accessibility bus डेस्कटॉप के
`at-spi` socket को बदल देगा (और बाहर निकलने पर हटा देगा)।

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`) |
| `--press <button>` | नहीं | इस label वाला बटन दबाएँ (केस-असंवेदनशील) |
| `--id <id>` | नहीं | `--press` के साथ: उत्तर देने के लिए डायलॉग (सूची से) |
| `--format <format>` | नहीं | `text`, `json` या `ndjson` |

## `commands`

हर कमांड उसके आर्ग्युमेंट, विकल्प और डिफ़ॉल्ट सहित, साथ में एग्ज़िट-कोड तालिका — स्वयं CLI से पढ़ी गई, इसलिए यह
कभी पुरानी नहीं पड़ती। एजेंट और टूलिंग के लिए अभिप्रेत।

```bash
unity-mcp-cli commands --format json
```

## `wait-for-ready`

तब तक प्रतीक्षा करें जब तक Unity Editor और MCP सर्वर कनेक्ट होकर टूल कॉल स्वीकार करने के लिए तैयार न हों। सर्वर को कॉन्फ़िगर करने योग्य अंतराल पर पोल करता है जब तक वह सफलतापूर्वक उत्तर न दे या टाइमआउट न पहुँचे। ऑटोमेशन स्क्रिप्ट और AI एजेंट ऑर्केस्ट्रेशन के लिए उपयोगी, जहाँ `open` Unity लॉन्च करता है लेकिन एजेंट को यह जानना होता है कि वह कब टूल कॉल करना शुरू कर सकता है।

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`) — कॉन्फ़िग पढ़ने और पोर्ट का पता लगाने के लिए उपयोग होता है |
| `--url <url>` | नहीं | सीधा सर्वर URL ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--token <token>` | नहीं | Bearer token ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--timeout <ms>` | नहीं | मिलीसेकंड में अधिकतम प्रतीक्षा समय (डिफ़ॉल्ट: 120000) |
| `--interval <ms>` | नहीं | मिलीसेकंड में पोलिंग अंतराल (डिफ़ॉल्ट: 3000) |
| `--format <format>` | नहीं | `text` (डिफ़ॉल्ट), `json` या `ndjson` |

तैयार होने पर `0`, टाइमआउट पर `14` और जब कोई मोडल डायलॉग एडिटर को अवरुद्ध करता है तो `18` पर बाहर निकलता है।

**उदाहरण — डिफ़ॉल्ट टाइमआउट (120s) के साथ प्रतीक्षा करें:**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**उदाहरण — CI के लिए छोटा टाइमआउट:**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**उदाहरण — स्पष्ट सर्वर URL:**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

## `setup-mcp`

AI एजेंट के लिए MCP कॉन्फ़िग फ़ाइलें लिखें, जिससे Unity Editor UI के बिना headless/CI सेटअप संभव हो। `setup-mcp --list` में दिखने वाले सभी एजेंट (Claude Code, Cursor, Gemini, Codex और अन्य) समर्थित हैं।

कॉन्फ़िग एक प्रति-प्रोजेक्ट URL (http ट्रांसपोर्ट के लिए `https://ai-game.dev/mcp/p/<pin>`, या stdio के लिए `project=<pin>` आर्ग्युमेंट) के ज़रिए इस प्रोजेक्ट पर **पिन** होता है। Cloud http ट्रांसपोर्ट के लिए इसमें हर एजेंट के लिए इस प्रोजेक्ट की **project key** (`Authorization: Bearer agd_pk_…`) भी होती है: एक कभी समाप्त न होने वाला, रद्द करने योग्य क्रेडेंशियल जो केवल इस प्रोजेक्ट से बँधा है, `~/.ai-game-dev/project-keys.json` से फिर से उपयोग किया जाता है या उस खाते से बनाया जाता है जिससे आपने [`login`](#login) के ज़रिए साइन इन किया। साइन-इन के बिना (या यदि key प्राप्त नहीं की जा सकती) कॉन्फ़िग केवल-URL होता है और एजेंट अपने स्वयं के OAuth से साइन इन करता है। हमेशा केवल-URL कॉन्फ़िग लिखने के लिए `--oauth` पास करें, key बदलने के लिए `--regenerate-key` (इस प्रोजेक्ट के अन्य एजेंट कॉन्फ़िग जिनमें पिछली key है उन्हें नई key पर ले जाया जाता है और प्रिंट किया जाता है, फिर पिछली key रद्द कर दी जाती है; यदि उनमें से कोई फिर से नहीं लिखा जा सकता, तो पिछली key सक्रिय छोड़ दी जाती है और एक चेतावनी उसका नाम बताती है), इसके बजाय एक स्पष्ट PAT लिखने के लिए `--token`, या साझा, बिना पिन वाले एंडपॉइंट के लिए `--no-pin`। stdio और लोकल-सर्वर कॉन्फ़िग में कभी project key नहीं होती। Antigravity इंस्टॉल के आधार पर `~/.gemini/config/mcp_config.json` या `~/.gemini/antigravity/mcp_config.json` में से कोई एक पढ़ता है, इसलिए दोनों फ़ाइलें लिखी जाती हैं और लिखा गया हर पाथ प्रिंट किया जाता है; यदि उनमें से कोई नहीं लिखी जा सकती तो कमांड विफल होती है। यह आउटपुट Unity Editor के **Configure** बटन से मेल खाता है।

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[agent-id]` | हाँ | कॉन्फ़िगर करने के लिए एजेंट (सभी देखने के लिए `--list` का उपयोग करें) |
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (डिफ़ॉल्ट रूप से cwd) |
| `--transport <transport>` | नहीं | ट्रांसपोर्ट मेथड: `stdio` या `http` (डिफ़ॉल्ट: `http`) |
| `--url <url>` | नहीं | सर्वर URL ओवरराइड (http ट्रांसपोर्ट के लिए) |
| `--token <token>` | नहीं | स्पष्ट PAT — project key के बजाय `Authorization` हेडर के रूप में लिखा जाता है |
| `--oauth` | नहीं | project key के बजाय केवल-URL Cloud कॉन्फ़िग लिखें (एजेंट अपने स्वयं के OAuth से साइन इन करता है) |
| `--regenerate-key` | नहीं | एक नई project key बनाएँ, कॉन्फ़िग फिर से लिखें, और पिछली key रद्द करें (केवल Cloud http) |
| `--no-pin` | नहीं | बिना पिन वाला URL लिखें / `project=` आर्ग्युमेंट छोड़ दें (डिफ़ॉल्ट: `/mcp/p/<pin>` के ज़रिए इस प्रोजेक्ट पर पिन करें) |
| `--list` | नहीं | सभी उपलब्ध एजेंट IDs सूचीबद्ध करें |

**उदाहरण — सभी समर्थित एजेंट सूचीबद्ध करें:**

```bash
unity-mcp-cli setup-mcp --list
```

**उदाहरण — stdio ट्रांसपोर्ट के साथ Cursor कॉन्फ़िगर करें:**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

**उदाहरण — इस प्रोजेक्ट की key बदलें (पुरानी key काम करना बंद कर देती है):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

**उदाहरण — बिना पिन वाला कॉन्फ़िग लिखें (साझा `/mcp` एंडपॉइंट):**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

## `setup-skills`

किसी AI एजेंट के लिए स्किल फ़ाइलें लिखें। हमेशा, ऑफ़लाइन (कोई एडिटर नहीं, कोई सर्वर नहीं): `unity-mcp-cli` स्किल, कि एक एजेंट
कोल्ड स्टार्ट से इस CLI का उपयोग कैसे करता है (पहली एडिटर कमांड एक हेडलेस एडिटर शुरू करती है; `rql` रेसिपी; `compile` / `test`;
`--format json`; एग्ज़िट कोड और `dialog`), गहराई के लिए `references/rql.md` और `references/install.md` के साथ, साथ ही
`ai-game-dev-app`। रेसिपी उसी सोर्स से आती हैं जिससे `--help`, इसलिए दोनों में अंतर नहीं आ सकता। फिर, जब
प्रोजेक्ट का एडिटर उत्तर देता है, तो यह प्लगइन से उसकी प्रति-टूल स्किल्स भी माँगता है; बंद एडिटर कोई एरर नहीं है।

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[agent-id]` | हाँ | जिस एजेंट के लिए स्किल्स जनरेट करनी हैं (सभी देखने के लिए `--list` का उपयोग करें) |
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (डिफ़ॉल्ट रूप से cwd) |
| `--url <url>` | नहीं | सर्वर URL ओवरराइड |
| `--token <token>` | नहीं | Auth token ओवरराइड |
| `--list` | नहीं | स्किल्स समर्थन स्थिति के साथ सभी एजेंट सूचीबद्ध करें |
| `--offline` | नहीं | केवल `unity-mcp-cli` स्किल लिखें; एडिटर से न पूछें |
| `--timeout <ms>` | नहीं | अनुरोध टाइमआउट मिलीसेकंड में (डिफ़ॉल्ट: 60000) |

**उदाहरण — स्किल्स समर्थन वाले एजेंट सूचीबद्ध करें:**

```bash
unity-mcp-cli setup-skills --list
```

## `remove-plugin`

Unity-MCP प्लगइन को किसी Unity प्रोजेक्ट की `Packages/manifest.json` से हटाएँ।

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | हाँ | Unity प्रोजेक्ट का पाथ (positional या `--path`) |

यह कमांड:
1. `com.ivanmurzak.unity.mcp` को `dependencies` से हटाती है
2. **Scoped registries और scopes को बनाए रखती है** — अन्य पैकेज उन पर निर्भर हो सकते हैं
3. यदि प्लगइन इंस्टॉल नहीं है तो **कुछ नहीं करती**

> यह कमांड चलाने के बाद, बदलाव लागू करने के लिए प्रोजेक्ट को Unity Editor में खोलें।

## `status`

Unity Editor और MCP सर्वर कनेक्शन स्थिति जाँचें। दिखाता है कि Unity चल रहा है या नहीं, लोकल MCP सर्वर पहुँच योग्य है या नहीं, और कॉन्फ़िग से रिज़ॉल्व किया गया सर्वर (जैसे क्लाउड) पहुँच योग्य है या नहीं।

```bash
unity-mcp-cli status ./MyGame
```

| विकल्प | आवश्यक | विवरण |
|---|---|---|
| `[path]` | नहीं | Unity प्रोजेक्ट पाथ (positional या `--path`) |
| `--url <url>` | नहीं | सीधा सर्वर URL ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--token <token>` | नहीं | Bearer token ओवरराइड (कॉन्फ़िग को बायपास करता है) |
| `--timeout <ms>` | नहीं | मिलीसेकंड में probe टाइमआउट (डिफ़ॉल्ट: 5000) |
| `--format <format>` | नहीं | `text` (डिफ़ॉल्ट), `json` या `ndjson` |

जब MCP सर्वर उत्तर देता है तो `0`, जब नहीं देता तो `7`, और जब कोई मोडल डायलॉग एडिटर को अवरुद्ध करता है तो `18` पर बाहर निकलता है (वह
आउटपुट में सूचीबद्ध होता है; देखें [`dialog`](#dialog))।

**उदाहरण आउटपुट:**

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

## Global Options

ये विकल्प सभी कमांड्स पर उपलब्ध हैं:

| विकल्प | विवरण |
|---|---|
| `-v, --verbose` | समस्या निवारण के लिए विस्तृत डायग्नोस्टिक आउटपुट सक्षम करें |
| `--version` | CLI वर्ज़न दिखाएँ |
| `--help` | कमांड के लिए सहायता दिखाएँ |

**उदाहरण — कोई भी कमांड विस्तृत आउटपुट के साथ चलाएँ:**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

## Exit Codes and Machine-Readable Output

`unity-mcp-cli`, `unreal-mcp-cli` और `godot-cli` एक ही एग्ज़िट-कोड तालिका साझा करते हैं, ताकि कोई स्क्रिप्ट या एजेंट संदेश पढ़े बिना "कमांड लाइन
ठीक करें", "एडिटर विफल हुआ" और "टेस्ट विफल हुए" में अंतर कर सके:

| कोड | नाम | अर्थ |
|---|---|---|
| `0` | `SUCCESS` | कमांड सफल हुई |
| `1` | `ERROR` | अवर्गीकृत एरर |
| `2` | `USAGE` | गलत आर्ग्युमेंट, अज्ञात टूल; कुछ नहीं चला। कमांड लाइन ठीक करें — इसे बिना बदले दोबारा न चलाएँ |
| `6` | `COMMAND_FAILED` | कमांड चली और विफल हुई (टूल एरर, कंपाइल एरर, बिना निर्णय वाला टेस्ट रन, अस्वीकृत क्रेडेंशियल) |
| `7` | `UNREACHABLE` | एडिटर या MCP सर्वर शुरू नहीं हो सका या उस तक पहुँचा नहीं जा सका; एनवायरनमेंट ठीक होने के बाद दोबारा प्रयास करें |
| `8` | `TESTS_FAILED` | टेस्ट चले और कुछ विफल हुए |
| `14` | `TIMEOUT` | परिणाम से पहले समय समाप्त हो गया |
| `17` | `BUSY` | एडिटर व्यस्त है और उसने कॉल नहीं चलाई; बाद में दोबारा प्रयास करें |
| `18` | `AWAITING_INPUT` | कोई मोडल डायलॉग एडिटर को अवरुद्ध करता है; किसी व्यक्ति को उसका उत्तर देना होगा (देखें [`dialog`](#dialog)) |

`--format json` (`run-tool`, `run-system-tool`, `exec`, `test`, `compile`, `dialog`, `status`, `wait-for-ready` और
`commands` पर) stdout पर ठीक एक परिणाम envelope प्रिंट करता है; बाकी सब stderr पर जाता है। `--format ndjson`
वही envelope एक पंक्ति के रूप में प्रिंट करता है और stderr पर प्रगति पंक्तियाँ (`{"type":"progress",...}`) स्ट्रीम करता है। `--format json` के अंतर्गत
कमांड-लाइन एरर फिर भी एक envelope प्रिंट करता है (कोड `USAGE`)।

```json
{
  "success": false,
  "command": "test",
  "data": { "verdict": "failed", "summary": { "total": 3, "passed": 2, "failed": 1 } },
  "errors": [{ "code": "TESTS_FAILED", "message": "1 test(s) failed." }],
  "warnings": []
}
```

`errors[].code` एक स्थिर टोकन है (`USAGE`, `NOT_FOUND`, `NOT_AUTHENTICATED`, `TOOL_FAILED`, `UNREACHABLE`, `TIMEOUT`,
`BUSY`, `AWAITING_INPUT`, `TESTS_FAILED`, `TEST_RUN_ERROR`, `TEST_TIMED_OUT`, `COMPILATION_FAILED`, `PROJECT_OPEN`,
`ERROR`, ...); इसी से मिलान करें, कभी संदेश से नहीं।

**सफल न होने वाली टूल कॉल** (`run-tool`, `run-system-tool`, `exec`, `rql`, `tools`) तीनों CLIs में एक ही एग्ज़िट कोड
और एरर कोड पर मैप होती है (तालिका `@baizor/gamedev-cli-core` में है; `commands --format json` इसे
`errorCodes` के रूप में सूचीबद्ध करता है)। जब MCP सर्वर ने उत्तर दिया, तो उसका HTTP स्टेटस निर्णय करता है, और envelope एरर उसे साथ रखता है:
`errors[{code, message, httpStatus}]`।

| स्थिति | HTTP स्टेटस | Exit | `errors[].code` |
|---|---|---|---|
| गलत आर्ग्युमेंट: गलत फ़्लैग, ऐसे टूल आर्ग्युमेंट जिन्हें CLI या सर्वर ने अस्वीकार किया (यहाँ सूचीबद्ध न किया गया कोई भी 4xx) | 400 (एक 422 `[invalid_params]`) | `2` | `USAGE` |
| उस नाम का कोई टूल नहीं (उन्हें `tools` से सूचीबद्ध करें), या पाथ पर कोई प्रोजेक्ट नहीं | 404 | `2` | `NOT_FOUND` |
| कोई उपयोग योग्य क्रेडेंशियल नहीं, या सर्वर ने उन्हें अस्वीकार किया: `login` से साइन इन करें | 401, 403 | `6` | `NOT_AUTHENTICATED` |
| टूल चला और उसने एरर रिपोर्ट किया (`status: "error"`, यहाँ सूचीबद्ध न किया गया कोई भी 5xx) | 409, 422, 500 | `6` | `TOOL_FAILED` |
| प्रोजेक्ट का कोड बिल्ड नहीं हुआ, इसलिए कोई एडिटर शुरू नहीं किया गया | — | `6` | `COMPILATION_FAILED` |
| कनेक्शन अस्वीकृत या रीसेट, सर्वर से कोई engine प्लगइन कनेक्ट नहीं, कॉल भेजे जाने के बाद प्लगइन खो गया | 502 | `7` | `UNREACHABLE` |
| समय पर कोई उत्तर नहीं: `--timeout`, या सर्वर का अपना टूल टाइमआउट | 408, 504 | `14` | `TIMEOUT` |
| व्यस्त और कॉल नहीं चलाई (रीलोड हो रहा, rate-limited, कोई अन्य CLI कॉल एडिटर को पकड़े हुए); बाद में दोबारा प्रयास करें | 503, 429 | `17` | `BUSY` |
| कोई मोडल डायलॉग एडिटर को अवरुद्ध करता है | — | `18` | `AWAITING_INPUT` |
| बाकी कुछ भी (कॉल रद्द की गई, एक अप्रत्याशित एरर) | — | `1` | `ERROR` |

# Full Automation Example

एक ही स्क्रिप्ट में शुरू से एक पूरा Unity MCP प्रोजेक्ट सेट अप करें:

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Sign in to ai-game.dev (browser OAuth; credential saved to ~/.ai-game-dev)
unity-mcp-cli login

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Check that the scripts compile, then close the editor
unity-mcp-cli compile ./MyAIGame
unity-mcp-cli close ./MyAIGame

# 8. Run the tests in a batch-mode editor (no plugin needed); JUnit for the CI dashboard
unity-mcp-cli test ./MyAIGame --junit-output junit.xml
```

# How It Works

### निर्धारित (Deterministic) पोर्ट

CLI हर Unity प्रोजेक्ट के लिए उसके डायरेक्टरी पाथ के आधार पर एक **निर्धारित पोर्ट** जनरेट करता है (SHA256 hash को पोर्ट रेंज 20000–29999 पर मैप किया जाता है)। यह Unity प्लगइन में पोर्ट जनरेशन से मेल खाता है, जिससे सर्वर और प्लगइन बिना मैनुअल कॉन्फ़िगरेशन के स्वचालित रूप से एक ही पोर्ट पर सहमत होते हैं।

### प्लगइन इंस्टॉलेशन

`install-plugin` कमांड `Packages/manifest.json` को सीधे संशोधित करती है:
- [OpenUPM](https://openupm.com/) scoped registry (`package.openupm.com`) जोड़ती है
- सभी आवश्यक scopes (`com.ivanmurzak`, `extensions.unity`) रजिस्टर करती है
- वर्ज़न-जागरूक अपडेट के साथ `com.ivanmurzak.unity.mcp` डिपेंडेंसी जोड़ती है (कभी डाउनग्रेड नहीं करती)

### कॉन्फ़िगरेशन फ़ाइल

`configure` कमांड `UserSettings/AI-Game-Developer-Config.json` पढ़ती और लिखती है, जो नियंत्रित करती है:
- **Tools** — AI एजेंट के लिए उपलब्ध MCP टूल्स
- **Prompts** — LLM बातचीत में डाले जाने वाले पूर्व-परिभाषित प्रॉम्प्ट्स
- **Resources** — AI एजेंट के लिए उजागर किया गया केवल-पढ़ने योग्य डेटा
- **कनेक्शन सेटिंग्स** — host URL, auth token, ट्रांसपोर्ट मेथड, टाइमआउट

### Unity Hub एकीकरण

जो कमांड्स एडिटर प्रबंधित करती हैं या प्रोजेक्ट बनाती हैं वे **Unity Hub CLI** (`--headless` मोड) का उपयोग करती हैं। यदि Unity Hub इंस्टॉल नहीं है, तो CLI **उसे स्वचालित रूप से डाउनलोड और इंस्टॉल करता है**:
- **Windows** — `UnityHubSetup.exe /S` के ज़रिए साइलेंट इंस्टॉल (administrator विशेषाधिकार की आवश्यकता हो सकती है)
- **macOS** — DMG डाउनलोड करता है, उसे माउंट करता है, और `Unity Hub.app` को `/Applications` में कॉपी करता है
- **Linux** — `UnityHub.AppImage` को `~/Applications/` में डाउनलोड करता है

# Library API

CLI बाइनरी के अतिरिक्त, `unity-mcp-cli` अपनी मुख्य कमांड्स को एक टाइप्ड, side-effect-मुक्त लाइब्रेरी के रूप में उजागर करता है ताकि अन्य Node.js / TypeScript टूल्स शेल को बुलाए बिना वही install / configure फ़्लो एम्बेड कर सकें।

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: 'x.y.z',        // optional — defaults to latest from OpenUPM
  onProgress: (event) => {
    // phase is one of: 'start' | 'dependencies-resolved' | 'manifest-patched' | 'done'
    console.log(event.phase, event.message);
  },
});

if (!result.success) {
  console.error('Install failed:', result.error?.message);
  return;
}

console.log(`Installed v${result.installedVersion}`);
for (const warning of result.warnings) console.warn(warning);
for (const step of result.nextSteps) console.log(step);
```

हर फ़ंक्शन एक टाइप्ड `{ success, ... }` परिणाम ऑब्जेक्ट लौटाता है; एरर कभी सार्वजनिक सीमा के पार नहीं फेंके जाते। लाइब्रेरी एंट्री में कोई top-level side effects नहीं हैं — `import 'unity-mcp-cli'` कभी argv पार्स नहीं करता और कभी stdout या stderr पर नहीं लिखता।

निर्यात किए गए फ़ंक्शन और टाइप्स की पूरी सूची के लिए [`CHANGELOG.md`](../CHANGELOG.md) देखें।
