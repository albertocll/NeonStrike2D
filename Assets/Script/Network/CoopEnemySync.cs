using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

// Enemigos compartidos en cooperativo (issue #206), con el anfitrion como autoridad.
// - El anfitrion simula todos los enemigos y jefes como en solitario. Cada SendInterval manda su
//   estado (posicion, vida, orientacion y color) y avisa de cada muerte, de cada bala o proyectil
//   enemigo y del dano que recibe la copia del invitado (PlayerHealth.MakeRemoteProxy).
// - El invitado no crea enemigos (WaveManager): crea copias sin IA (NetEnemy) que siguen ese
//   estado. Sus balas no hacen dano a las copias: mandan el impacto al anfitrion, que lo aplica.
// Va por el canal generico SendGameState, que el servidor reenvia tal cual al otro jugador.
public class CoopEnemySync : MonoBehaviour
{
    public static CoopEnemySync Instance { get; private set; }
    public static bool IsGuest => Instance != null && !GameData.IsHost;

    private const float SendInterval = 0.1f;
    // El servidor lee cada mensaje en un buffer de 4096 bytes: 20 enemigos por mensaje caben de sobra.
    private const int EnemiesPerMessage = 20;

    private const string KindSnapshot = "es";
    private const string KindDeaths = "ed";
    private const string KindHit = "eh";
    private const string KindBullet = "eb";
    private const string KindFlak = "ef";
    private const string KindPlayerDamage = "pd";
    private const string KindDown = "dn";
    private const string KindRevive = "rv";

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private string roomId;
    private float nextSendTime;
    private int nextId = 1;
    private WaveManager waveManager;

    private readonly Dictionary<int, NetEnemy> enemies = new Dictionary<int, NetEnemy>();
    private readonly List<NetEnemy> alive = new List<NetEnemy>();
    private readonly List<int> deadIds = new List<int>();
    private readonly StringBuilder sb = new StringBuilder();

    // Invitado: prefabs de enemigos, balas y proyectiles por nombre.
    private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private readonly HashSet<string> missingKeys = new HashSet<string>();

    [Serializable]
    private class CoopMessage
    {
        public string kind;
        public string d;
    }

    public void Init(string room)
    {
        roomId = room;
        Instance = this;
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnReceiveGameState += OnReceiveGameState;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnReceiveGameState -= OnReceiveGameState;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        if (IsGuest) BuildPrefabRegistry();
    }

    // ── Anfitrion: envio ─────────────────────────────────────────────────────

    private void Update()
    {
        if (!GameData.IsHost || Time.unscaledTime < nextSendTime) return;
        nextSendTime = Time.unscaledTime + SendInterval;

        TrackNewEnemies();
        SendDeaths();
        SendSnapshot();
    }

    private void TrackNewEnemies()
    {
        foreach (var c in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Track(c.gameObject);
        foreach (var b in FindObjectsByType<BossBase>(FindObjectsSortMode.None)) Track(b.gameObject);
        foreach (var b in FindObjectsByType<BossSentinelController>(FindObjectsSortMode.None)) Track(b.gameObject);
    }

    private void Track(GameObject go)
    {
        if (go.TryGetComponent(out NetEnemy _)) return;
        var net = go.AddComponent<NetEnemy>();
        net.InitHost(nextId++);
        enemies[net.Id] = net;
    }

    private void SendDeaths()
    {
        deadIds.Clear();
        alive.Clear();
        foreach (var pair in enemies)
        {
            if (pair.Value == null || pair.Value.IsDead) deadIds.Add(pair.Key);
            else alive.Add(pair.Value);
        }
        if (deadIds.Count == 0) return;

        // id, con una g detras si lo ha matado el invitado (para que sume el los puntos).
        sb.Clear();
        foreach (int id in deadIds)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(id);
            NetEnemy dead = enemies[id];
            if (dead != null && dead.KilledByGuest) sb.Append('g');
            enemies.Remove(id);
        }
        Send(KindDeaths, sb.ToString());
    }

    // oleada|id,clave,x,y,vida,banderas,color;... en trozos de EnemiesPerMessage.
    private void SendSnapshot()
    {
        int wave = waveManager != null ? waveManager.CurrentWave : 0;
        int i = 0;
        do
        {
            sb.Clear();
            sb.Append(wave).Append('|');
            for (int n = 0; n < EnemiesPerMessage && i < alive.Count; n++, i++)
            {
                if (n > 0) sb.Append(';');
                alive[i].AppendState(sb);
            }
            Send(KindSnapshot, sb.ToString());
        } while (i < alive.Count);
    }

    public void ReportEnemyBullet(Bullet bullet)
    {
        if (!GameData.IsHost) return;
        Vector2 p = bullet.transform.position;
        Vector2 dir = bullet.Direction;
        Send(KindBullet, string.Join(",", NetEnemy.PrefabKey(bullet.name), NetEnemy.F(p.x), NetEnemy.F(p.y),
            dir.x.ToString("0.###", Inv), dir.y.ToString("0.###", Inv), NetEnemy.F(bullet.speed), NetEnemy.F(bullet.lifeTime)));
    }

    public void ReportFlakShell(FlakShell shell, Vector2 start, Vector2 target)
    {
        if (!GameData.IsHost) return;
        Send(KindFlak, string.Join(",", NetEnemy.PrefabKey(shell.name), NetEnemy.F(start.x), NetEnemy.F(start.y),
            NetEnemy.F(target.x), NetEnemy.F(target.y)));
    }

    public void SendPlayerDamage(int amount)
    {
        if (!GameData.IsHost) return;
        Send(KindPlayerDamage, amount.ToString(Inv));
    }

    // ── Invitado: envio ──────────────────────────────────────────────────────

    public void SendHit(int id, int damage, Vector2 dir)
    {
        if (GameData.IsHost) return;
        Send(KindHit, string.Join(",", id.ToString(Inv), damage.ToString(Inv),
            dir.x.ToString("0.###", Inv), dir.y.ToString("0.###", Inv)));
    }

    private void Send(string kind, string data)
    {
        if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected) return;
        // data solo lleva numeros, letras y , ; | . - : no hace falta escaparlo.
        string json = "{\"kind\":\"" + kind + "\",\"d\":\"" + data + "\"}";
        _ = NetworkManager.Instance.SendGameStateAsync(roomId, json);
    }

    // ── Recepcion ────────────────────────────────────────────────────────────

    private void OnReceiveGameState(string json)
    {
        CoopMessage msg;
        try { msg = JsonUtility.FromJson<CoopMessage>(json); }
        catch (ArgumentException) { return; }
        if (msg == null || string.IsNullOrEmpty(msg.d)) return;

        try
        {
            switch (msg.kind)
            {
                case KindSnapshot: if (IsGuest) OnSnapshot(msg.d); break;
                case KindDeaths: if (IsGuest) OnDeaths(msg.d); break;
                case KindBullet: if (IsGuest) OnBullet(msg.d); break;
                case KindFlak: if (IsGuest) OnFlak(msg.d); break;
                case KindPlayerDamage: if (IsGuest) OnPlayerDamage(msg.d); break;
                case KindHit: if (GameData.IsHost) OnHit(msg.d); break;
                case KindDown: OnRemoteDowned(); break;
                case KindRevive: if (IsGuest) ReviveBoth(); break;
            }
        }
        catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException || e is ArgumentException)
        {
            Debug.LogWarning($"[CoopEnemySync] Mensaje {msg.kind} mal formado: {msg.d}");
        }
    }

    private void OnSnapshot(string data)
    {
        int bar = data.IndexOf('|');
        int wave = int.Parse(data.Substring(0, bar), Inv);
        if (waveManager != null && wave > 0) waveManager.ShowRemoteWave(wave);

        foreach (string entry in data.Substring(bar + 1).Split(';'))
        {
            if (entry.Length == 0) continue;
            string[] f = entry.Split(',');
            int id = int.Parse(f[0], Inv);
            var pos = new Vector2(float.Parse(f[2], Inv), float.Parse(f[3], Inv));

            if (!enemies.TryGetValue(id, out NetEnemy net) || net == null)
            {
                net = SpawnPuppet(f[1], pos, id);
                if (net == null) continue;
            }

            if (!ColorUtility.TryParseHtmlString("#" + f[6], out Color color)) color = Color.white;
            net.ApplyRemoteState(pos, int.Parse(f[4], Inv), int.Parse(f[5], Inv), color);
        }
    }

    private void OnDeaths(string data)
    {
        foreach (string s in data.Split(','))
        {
            bool killedByGuest = s.EndsWith("g");
            int id = int.Parse(killedByGuest ? s.Substring(0, s.Length - 1) : s, Inv);
            if (enemies.TryGetValue(id, out NetEnemy net))
            {
                if (net != null) net.RemoteDie(killedByGuest);
                enemies.Remove(id);
            }
        }
    }

    private void OnBullet(string data)
    {
        string[] f = data.Split(',');
        var prefab = Prefab(f[0])?.GetComponent<Bullet>();
        if (prefab == null) return;

        var pos = new Vector2(float.Parse(f[1], Inv), float.Parse(f[2], Inv));
        var dir = new Vector2(float.Parse(f[3], Inv), float.Parse(f[4], Inv));
        Bullet b = Instantiate(prefab, pos, Quaternion.identity);
        b.MakeVisualOnly();
        b.speed = float.Parse(f[5], Inv);
        b.lifeTime = float.Parse(f[6], Inv);
        b.Init(dir);
    }

    private void OnFlak(string data)
    {
        string[] f = data.Split(',');
        var prefab = Prefab(f[0])?.GetComponent<FlakShell>();
        if (prefab == null) return;

        var start = new Vector2(float.Parse(f[1], Inv), float.Parse(f[2], Inv));
        var target = new Vector2(float.Parse(f[3], Inv), float.Parse(f[4], Inv));
        FlakShell shell = Instantiate(prefab, start, Quaternion.identity);
        shell.MakeVisualOnly();
        shell.Init(target);
    }

    private void OnPlayerDamage(string data)
    {
        if (PlayerTargets.Local == null) return;
        var health = PlayerTargets.Local.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(int.Parse(data, Inv));
    }

    private void OnHit(string data)
    {
        string[] f = data.Split(',');
        int id = int.Parse(f[0], Inv);
        if (!enemies.TryGetValue(id, out NetEnemy net) || net == null) return;
        net.ApplyGuestHit(int.Parse(f[1], Inv), new Vector2(float.Parse(f[2], Inv), float.Parse(f[3], Inv)));
    }

    // ── Muerte en cooperativo (#296) ─────────────────────────────────────────
    // Quien muere con el otro vivo queda fuera de combate hasta el final de la oleada y vuelve con la
    // mitad de la vida. Cuando caen los dos, la partida termina en los dos clientes.

    private bool bothDown;

    // TryGetComponent devuelve null de verdad (GetComponent da un null falso en el editor y ?. fallaria).
    private static PlayerHealth HealthOf(Transform t) => t != null && t.TryGetComponent(out PlayerHealth h) ? h : null;

    // Lo llama PlayerHealth al morir. true = queda fuera de combate; false = fin de la partida.
    public bool OnLocalDowned()
    {
        Send(KindDown, "1");
        if (PlayerTargets.RemoteDown)
        {
            bothDown = true;
            return false;
        }
        PlayerTargets.LocalDown = true;
        return true;
    }

    private void OnRemoteDowned()
    {
        PlayerTargets.RemoteDown = true;
        HealthOf(PlayerTargets.Remote)?.ShowRemoteDowned(true);

        // Este ya estaba caido: han caido los dos.
        var local = HealthOf(PlayerTargets.Local);
        if (local != null && local.IsDowned && !bothDown)
        {
            bothDown = true;
            local.EndDowned();
        }
    }

    // Anfitrion: WaveManager avisa al limpiar la oleada; los dos vuelven a la partida.
    public void OnWaveCleared()
    {
        if (!GameData.IsHost || bothDown) return;
        ReviveBoth();
        Send(KindRevive, "1");
    }

    private void ReviveBoth()
    {
        if (bothDown) return;
        HealthOf(PlayerTargets.Local)?.Revive();
        PlayerTargets.LocalDown = false;
        if (PlayerTargets.RemoteDown) HealthOf(PlayerTargets.Remote)?.ShowRemoteDowned(false);
        PlayerTargets.RemoteDown = false;
    }
    // ── Invitado: copias ─────────────────────────────────────────────────────

    private NetEnemy SpawnPuppet(string key, Vector2 pos, int id)
    {
        GameObject prefab = Prefab(key);
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        var net = go.AddComponent<NetEnemy>();
        net.InitPuppet(id);
        enemies[id] = net;
        return net;
    }

    private GameObject Prefab(string key)
    {
        if (prefabs.TryGetValue(key, out GameObject prefab)) return prefab;
        if (missingKeys.Add(key))
            Debug.LogWarning($"[CoopEnemySync] El invitado no conoce el prefab '{key}'; no se puede mostrar.");
        return null;
    }

    // Enemigos del EnemySpawner de la escena y jefes de todos los niveles, y todo lo que estos
    // referencian (invocaciones, balas, proyectiles), buscado en sus campos serializados.
    private void BuildPrefabRegistry()
    {
        var pending = new Queue<GameObject>();
        foreach (var spawner in FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
            foreach (var p in spawner.AllPrefabs) pending.Enqueue(p);
        foreach (var level in LevelManager.Levels)
            if (level != null && level.bossPrefab != null) pending.Enqueue(level.bossPrefab);

        while (pending.Count > 0)
        {
            GameObject go = pending.Dequeue();
            if (go == null || prefabs.ContainsKey(go.name)) continue;
            prefabs[go.name] = go;

            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                foreach (GameObject referenced in ReferencedObjects(mb))
                    pending.Enqueue(referenced);
        }
    }

    private static IEnumerable<GameObject> ReferencedObjects(MonoBehaviour mb)
    {
        if (mb == null) yield break;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        for (Type t = mb.GetType(); t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            foreach (FieldInfo field in t.GetFields(Flags))
            {
                if (field.FieldType == typeof(GameObject))
                {
                    if (field.GetValue(mb) is GameObject go && go != null) yield return go;
                }
                else if (typeof(Component).IsAssignableFrom(field.FieldType))
                {
                    if (field.GetValue(mb) is Component c && c != null) yield return c.gameObject;
                }
            }
        }
    }
}
