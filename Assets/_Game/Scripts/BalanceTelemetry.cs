using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CraneMachine
{
    public class BalanceTelemetry : MonoBehaviour
    {
        [Header("Sampling")]
        [Tooltip("Seconds between telemetry samples.")]
        [SerializeField] private float sampleInterval = 1f;
        [Tooltip("Max samples kept in the rolling timeline.")]
        [SerializeField] private int maxSamples = 4000;

        [Header("File export")]
        [Tooltip("Written to Application.persistentDataPath. Press the export key or call ExportToFile().")]
        [SerializeField] private string fileName = "balance-telemetry.json";
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key exportKey = Key.F9;
#else
        [SerializeField] private KeyCode exportKey = KeyCode.F9;
#endif

        [Header("Live HTTP (optional)")]
        [Tooltip("Serve the JSON at http://localhost:<port>/telemetry for the dashboard to poll.")]
        [SerializeField] private bool serveHttp = true;
        [SerializeField] private int port = 8787;

        // ---- sampled timeline ----
        [Serializable]
        private struct Sample
        {
            public float t;          // seconds since play start
            public int money;
            public int purchases;    // total upgrade levels bought
            public int itemsUnlocked;
            public float moneyMult;
            public float incomePerSec; // derived (money delta / dt)
            // per-milestone unlock flags (true once reached)
            public bool banana, tinCan, teddy, diamond, magnet, conveyor, autoMagnet;
        }

        private readonly List<Sample> _samples = new List<Sample>();
        private float _elapsed;
        private float _sampleTimer;
        private int _lastMoney;
        private int _lastPurchases;

        private HttpListener _http;
        private Thread _httpThread;
        private volatile string _cachedJson = "{}";
        private readonly object _jsonLock = new object();

        private void OnEnable()
        {
            _elapsed = 0f;
            _sampleTimer = 0f;
            _samples.Clear();
            if (serveHttp) StartHttp();
        }

        private void OnDisable()
        {
            ExportToFile();
            StopHttp();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _sampleTimer += Time.deltaTime;

            if (ExportKeyPressed()) ExportToFile();

            if (_sampleTimer < sampleInterval) return;
            float dt = _sampleTimer;
            _sampleTimer = 0f;
            TakeSample(dt);
            RefreshCachedJson();
        }

        private bool ExportKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            // Guard against a stale/invalid serialized value (e.g. a KeyCode int
            // left over from before this field was a Key). Out-of-range throws otherwise.
            if (exportKey <= Key.None || exportKey >= Key.IMESelected) return false;
            return Keyboard.current[exportKey].wasPressedThisFrame;
#else
            return Input.GetKeyDown(exportKey);
#endif
        }

        private void TakeSample(float dt)
        {
            var stat = ServiceLocator.StatService;
            var ups = ServiceLocator.UpgradeService;
            if (stat == null) return;

            int money = stat.CurrentMoney;
            int purchases = CountPurchases(ups);
            int unlocked = CountUnlockedItems(stat);
            float mult = stat.GameValue(GameStat.MoneyMultiplier);
            float income = dt > 0f ? (money - _lastMoney) / dt : 0f;

            _samples.Add(new Sample
            {
                t = _elapsed, money = money, purchases = purchases,
                itemsUnlocked = unlocked, moneyMult = mult,
                incomePerSec = Mathf.Max(0f, income),
                banana   = ItemUnlocked(stat, "Banana"),
                tinCan   = ItemUnlocked(stat, "TinCan"),
                teddy    = ItemUnlocked(stat, "TeddyBear"),
                diamond  = ItemUnlocked(stat, "Diamond"),
                magnet   = TargetActive(UnlockTarget.Magnet),
                conveyor = TargetActive(UnlockTarget.Conveyor),
                autoMagnet = stat.GameValue(GameStat.AutoMagnet) > 0f,
            });
            if (_samples.Count > maxSamples) _samples.RemoveAt(0);

            _lastMoney = money;
            _lastPurchases = purchases;
        }

        // Unlock state of a specific item type by class name.
        private static bool ItemUnlocked(StatService stat, string typeName)
        {
            foreach (var t in ItemTypeCache())
                if (t.Name == typeName)
                    return stat.ItemValue(t, ItemStat.Unlocked) > 0f;
            return false;
        }

        // Whether a scene-unlock target (magnet, conveyor) is currently active.
        private static bool TargetActive(UnlockTarget target)
        {
            var go = SceneRef.Get(target);
            return go != null && go.activeSelf;
        }

        private static int CountPurchases(UpgradeService ups)
        {
            if (ups == null) return 0;
            int n = 0;
            foreach (var u in ups.AllUpgrades()) n += u.TimesPurchased;
            return n;
        }

        private static int CountUnlockedItems(StatService stat)
        {
            int n = 0;
            foreach (var t in ItemTypeCache())
                if (stat.ItemValue(t, ItemStat.Unlocked) > 0f) n++;
            return n;
        }

        // ---- item type discovery (cached) ----
        private static List<Type> _itemTypes;
        private static List<Type> ItemTypeCache()
        {
            if (_itemTypes != null) return _itemTypes;
            _itemTypes = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                    if (typeof(ItemType).IsAssignableFrom(t) && !t.IsAbstract)
                        _itemTypes.Add(t);
            }
            return _itemTypes;
        }

        // ================= JSON =================
        // Hand-rolled to avoid pulling in a serializer and to control the exact schema
        // the dashboard expects.
        public string BuildJson()
        {
            var stat = ServiceLocator.StatService;
            var ups = ServiceLocator.UpgradeService;
            var sb = new StringBuilder(4096);
            var ci = CultureInfo.InvariantCulture;

            sb.Append('{');

            // meta
            sb.Append("\"meta\":{");
            sb.AppendFormat(ci, "\"exportedAt\":\"{0:o}\",", DateTime.UtcNow);
            sb.AppendFormat(ci, "\"playtimeSeconds\":{0},", _elapsed.ToString("0.0", ci));
            sb.AppendFormat(ci, "\"source\":\"BalanceTelemetry\"");
            sb.Append("},");

            // config: upgrades
            sb.Append("\"upgrades\":[");
            if (ups != null)
            {
                bool first = true;
                foreach (var u in ups.AllUpgrades())
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('{');
                    sb.AppendFormat(ci, "\"name\":\"{0}\",", Esc(u.DisplayName));
                    sb.AppendFormat(ci, "\"type\":\"{0}\",", Esc(u.GetType().Name));
                    sb.AppendFormat(ci, "\"currentCost\":{0},", u.CurrentCost);
                    sb.AppendFormat(ci, "\"timesPurchased\":{0},", u.TimesPurchased);
                    sb.AppendFormat(ci, "\"maxedOut\":{0}", u.MaxedOut ? "true" : "false");
                    sb.Append('}');
                }
            }
            sb.Append("],");

            // config: items (live values, reflecting purchased upgrades)
            sb.Append("\"items\":[");
            if (stat != null)
            {
                bool first = true;
                foreach (var t in ItemTypeCache())
                {
                    var proto = (ItemType)Activator.CreateInstance(t);
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append('{');
                    sb.AppendFormat(ci, "\"name\":\"{0}\",", Esc(proto.DisplayName));
                    sb.AppendFormat(ci, "\"sellValue\":{0},", stat.ItemValue(t, ItemStat.SellValue).ToString("0.###", ci));
                    sb.AppendFormat(ci, "\"weight\":{0},", stat.ItemValue(t, ItemStat.Weight).ToString("0.###", ci));
                    sb.AppendFormat(ci, "\"mass\":{0},", stat.ItemValue(t, ItemStat.Mass).ToString("0.###", ci));
                    sb.AppendFormat(ci, "\"unlocked\":{0}", stat.ItemValue(t, ItemStat.Unlocked) > 0f ? "true" : "false");
                    sb.Append('}');
                }
            }
            sb.Append("],");

            // config: game stats snapshot
            sb.Append("\"stats\":{");
            if (stat != null)
            {
                bool first = true;
                foreach (GameStat g in Enum.GetValues(typeof(GameStat)))
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.AppendFormat(ci, "\"{0}\":{1}", g, stat.GameValue(g).ToString("0.####", ci));
                }
            }
            sb.Append("},");

            // telemetry timeline
            sb.Append("\"timeline\":[");
            for (int i = 0; i < _samples.Count; i++)
            {
                var s = _samples[i];
                if (i > 0) sb.Append(',');
                sb.Append('{');
                sb.AppendFormat(ci, "\"t\":{0},", s.t.ToString("0.0", ci));
                sb.AppendFormat(ci, "\"money\":{0},", s.money);
                sb.AppendFormat(ci, "\"purchases\":{0},", s.purchases);
                sb.AppendFormat(ci, "\"itemsUnlocked\":{0},", s.itemsUnlocked);
                sb.AppendFormat(ci, "\"moneyMult\":{0},", s.moneyMult.ToString("0.###", ci));
                sb.AppendFormat(ci, "\"incomePerSec\":{0},", s.incomePerSec.ToString("0.###", ci));
                sb.AppendFormat(ci, "\"f\":{{\"banana\":{0},\"tinCan\":{1},\"teddy\":{2},\"diamond\":{3},\"magnet\":{4},\"conveyor\":{5},\"autoMagnet\":{6}}}",
                    B(s.banana), B(s.tinCan), B(s.teddy), B(s.diamond), B(s.magnet), B(s.conveyor), B(s.autoMagnet));
                sb.Append('}');
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        private static string Esc(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string B(bool v) => v ? "true" : "false";

        private void RefreshCachedJson()
        {
            string json = BuildJson();
            lock (_jsonLock) _cachedJson = json;
        }

        // ================= File export =================
        public void ExportToFile()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, fileName);
                File.WriteAllText(path, BuildJson());
                Debug.Log($"[BalanceTelemetry] Exported to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceTelemetry] Export failed: {e.Message}");
            }
        }

        // ================= Live HTTP =================
        private void StartHttp()
        {
            if (!HttpListener.IsSupported)
            {
                Debug.LogWarning("[BalanceTelemetry] HttpListener unsupported on this platform; live serving off.");
                return;
            }
            try
            {
                _http = new HttpListener();
                _http.Prefixes.Add($"http://localhost:{port}/");
                _http.Start();
                _httpThread = new Thread(HttpLoop) { IsBackground = true };
                _httpThread.Start();
                Debug.Log($"[BalanceTelemetry] Live telemetry at http://localhost:{port}/telemetry");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceTelemetry] Could not start HTTP server: {e.Message}");
                _http = null;
            }
        }

        private void HttpLoop()
        {
            while (_http != null && _http.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = _http.GetContext(); }
                catch { break; }

                try
                {
                    // CORS so the browser dashboard can fetch across origin.
                    ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                    ctx.Response.ContentType = "application/json";

                    string body;
                    lock (_jsonLock) body = _cachedJson;

                    byte[] buf = Encoding.UTF8.GetBytes(body);
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                    ctx.Response.OutputStream.Close();
                }
                catch { /* client hung up */ }
            }
        }

        private void StopHttp()
        {
            try
            {
                if (_http != null) { _http.Stop(); _http.Close(); _http = null; }
                if (_httpThread != null && _httpThread.IsAlive) _httpThread.Join(200);
            }
            catch { }
        }

        private void OnApplicationQuit() => StopHttp();
    }
}