using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// Serwer TCP uruchamiany wewn¹trz Unity.
/// Odpowiada za nawi¹zanie po³¹czenia ze skryptem Python oraz wymianê danych.
/// Dzia³a na osobnym w¹tku ¿eby nie blokowaæ g³ównej pêtli gry.
/// </summary>
public class AgentServer : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5005; // Port agentow (P1: 5005, P2: 5006)
    public bool isConnected = false;

    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private Thread serverThread;

    // Kolejki bezpieczne w¹tkowo
    private ConcurrentQueue<string> actionsReceived = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> statesToSend = new ConcurrentQueue<string>();

    // Uruchomienie serwera w tle
    private void Start()
    {
        serverThread = new Thread(StartServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    /// <summary>
    /// G³ówna pêtla w¹tku sieciowego. 
    /// Akceptuje klienta i obs³uguje strumieñ danych.
    /// </summary>
    private void StartServer()
    {
        try
        {
            // Nas³uch na localhost (127.0.0.1)
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            server.Start();

            // Oczekiwanie na po³¹czenie blokujac watek do momentu polaczenia z pythonem
            client = server.AcceptTcpClient();
            stream = client.GetStream();
            isConnected = true;
            Debug.Log($"[Agent Port {port}] - Connected");

            byte[] buffer = new byte[1024];
            while (isConnected)
            {
                // Odczyt danych
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    foreach (char c in message)
                    {
                        if (char.IsDigit(c)) actionsReceived.Enqueue(c.ToString());
                    }
                }

                // Wyslanie dancyh
                if (statesToSend.TryDequeue(out string stateJson))
                {
                    byte[] data = Encoding.UTF8.GetBytes(stateJson + "\n");
                    stream.Write(data, 0, data.Length);
                }

                // Zabezpieczenie przed obciazeniem CPU
                Thread.Sleep(5);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Agent Port {port}] Error: {e.Message}");
        }
    }

    /// <summary>
    /// Dodaje stan do kolejki wysy³kowej
    /// </summary>
    /// <param name="jsonState">Stany zapisane w .json</param>
    public void SendState(string jsonState)
    {
        if (isConnected) statesToSend.Enqueue(jsonState);
    }

    /// <summary>
    /// Pobiera najnowsz¹ akcjê z kolejki
    /// </summary>
    /// <returns>String reprezentuj¹cy ID akcji lub null - brak nowych akcji.</returns>
    public string GetLatestAction()
    {
        if (actionsReceived.TryDequeue(out string action)) return action;
        return null;
    }

    /// <summary>
    /// Zamyka polaczenie i watki przy wylaczeniu gry
    /// </summary>
    private void OnApplicationQuit()
    {
        isConnected = false;
        if (server != null) server.Stop();
        if (serverThread != null) serverThread.Abort();
    }
}