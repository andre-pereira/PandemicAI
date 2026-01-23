

// OBSNativeConnectorFixed.cs
// Minimal: connect -> identify -> StartRecord ; On quit -> StopRecord
// Requires: Newtonsoft.Json (Window > Package Manager > add com.unity.nuget.newtonsoft-json)
using System;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
public class OBSNativeConnectorFixed : MonoBehaviour
{
    [Header("OBS WebSocket")]
    [SerializeField] string serverAddress = "ws://127.0.0.1:4455"; // prefer 127.0.0.1 over 'localhost'
    [SerializeField] string serverPassword = "roboticslab";
    ClientWebSocket ws;
    CancellationTokenSource cts;
    bool identified;
    void Awake() => DontDestroyOnLoad(gameObject);
    async void OnEnable()
    {
        Application.runInBackground = true;
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();
        identified = false;
        try
        {
            Debug.Log($"[OBS] Connecting to {serverAddress}...");
            await ws.ConnectAsync(new Uri(serverAddress), cts.Token);
            _ = ReceiveLoop(); // start background receiver
        }
        catch (Exception ex)
        {
            Debug.LogError("[OBS] Connect failed: " + ex.Message);
        }
    }
    async void OnApplicationQuit()
    {
        try
        {
            if (identified) await SendRequest("StopRecord");
            if (ws != null && ws.State == WebSocketState.Open)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { /* ignore */ }
        finally
        {
            cts?.Cancel();
            ws?.Dispose();
        }
    }
    // ---- Receive loop that assembles full messages ----
    async Task ReceiveLoop()
    {
        var buffer = new byte[16384];
        var builder = new StringBuilder(4096);
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                builder.Length = 0;
                WebSocketReceiveResult res;
                do
                {
                    res = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                    if (res.MessageType == WebSocketMessageType.Close)
                        return;
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));
                } while (!res.EndOfMessage);
                var text = builder.ToString();
                // Debug.Log("[OBS] << " + text);
                HandleMessage(text);
            }
        }
        catch (OperationCanceledException)
        {
            // ignore on shutdown
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[OBS] ReceiveLoop error: " + ex.Message);
        }
    }
    void HandleMessage(string json)
    {
        var jo = JObject.Parse(json);
        int op = (int)jo["op"];
        var d = (JObject)jo["d"];
        switch (op)
        {
            case 0:
                { // Hello
                    Debug.Log("[OBS] Hello received");
                    string auth = null;
                    var a = d["authentication"] as JObject;
                    if (a != null)
                    {
                        var challenge = (string)a["challenge"];
                        var salt = (string)a["salt"];
                        auth = ComputeAuth(serverPassword, salt, challenge);
                    }
                    var identify = new JObject
                    {
                        ["op"] = 1,
                        ["d"] = new JObject
                        {
                            ["rpcVersion"] = 1,
                            // optionally subscribe to events; not required
                            ["authentication"] = auth
                        }
                    };
                    _ = SendText(identify.ToString());
                    break;
                }
            case 2:
                { // Identified
                    identified = true;
                    Debug.Log("[OBS] Identified → StartRecord");
                    _ = SendRequest("StartRecord");
                    _ = SendRequest("GetRecordStatus");
                    break;
                }
            case 5:
                { // Event
                    var evtType = (string)d["eventType"];
                    if (evtType == "RecordStateChanged")
                    {
                        bool active = (bool)d["eventData"]?["outputActive"];
                        string path = (string)d["eventData"]?["outputPath"];
                        Debug.Log($"[OBS] Record state: {(active ? "ACTIVE" : "INACTIVE")}  Path={path}");
                    }
                    break;
                }
            case 7:
                { // RequestResponse
                    string reqType = (string)d["requestType"];
                    bool ok = (bool)(d["requestStatus"]?["result"] ?? false);
                    string code = (string)d["requestStatus"]?["code"] ?? "";
                    if (ok) Debug.Log($"[OBS] {reqType} OK");
                    else Debug.LogWarning($"[OBS] {reqType} FAIL {(string.IsNullOrEmpty(code) ? "" : "(" + code + ")")}");
                    if (reqType == "GetRecordStatus" && ok)
                    {
                        bool active = (bool)d["responseData"]?["outputActive"];
                        string path = (string)d["responseData"]?["outputPath"];
                        Debug.Log($"[OBS] Recording active={active} path={path}");
                    }
                    break;
                }
            default:
                // ignore other ops
                break;
        }
    }
    // ---- Sending helpers ----
    async Task SendRequest(string requestType)
    {
        if (ws == null || ws.State != WebSocketState.Open || !identified) return;
        var req = new JObject
        {
            ["op"] = 6,
            ["d"] = new JObject
            {
                ["requestType"] = requestType,
                ["requestId"] = Guid.NewGuid().ToString(),
                ["requestData"] = new JObject()
            }
        };
        await SendText(req.ToString());
    }
    async Task SendText(string text)
    {
        var data = Encoding.UTF8.GetBytes(text);
        await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cts.Token);
    }
    static string ComputeAuth(string password, string salt, string challenge)
    {
        // auth = base64( SHA256( base64(SHA256(password + salt)) + challenge ) )
        using var sha = SHA256.Create();
        var secret = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
        var secretB64 = Convert.ToBase64String(secret);
        var authBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(secretB64 + challenge));
        return Convert.ToBase64String(authBytes);
    }
}