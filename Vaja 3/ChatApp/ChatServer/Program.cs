using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class ChatServer {
    #region Konstante
    const int STD_PORT = 1234;
    const string STD_IP = "127.0.0.1";
    const int STD_MSG_SIZE = 1024;
    const string STD_CRYPT_KEY = "123456789012345678901234";
    const string STD_CRYPT_IV = "12345678";
    #endregion

    static SortedList<string, TcpClient> clients = new SortedList<string, TcpClient>();

    static void Main(string[] args) {
        TcpListener server = new TcpListener(IPAddress.Parse(STD_IP), STD_PORT);
        server.Start();
        Console.WriteLine($"Strežnik teče na {STD_IP}:{STD_PORT}");

        while (true) {
            TcpClient client = server.AcceptTcpClient();
            Thread clientThread = new Thread(HandleClient);
            clientThread.Start(client);
        }
    }

    static void HandleClient(object obj) {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        string username = "";

        try {
            byte[] buffer = new byte[STD_MSG_SIZE];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            clients.Add(username, client);

            Broadcast($"* {username} se je pridružil klepetu.");

            while (true) {
                buffer = new byte[STD_MSG_SIZE];
                bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0) {
                    break;
                }

                // DEŠIFRIRANJE
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine(message);
                Broadcast($"{username} pravi: {Decrypt(message)}");
            }
        } catch {
            Console.WriteLine($"{username} se je odjavil.");
        } finally {
            if (clients.ContainsKey(username)) {
                clients.Remove(username);
            }
            Broadcast($"* {username} je zapustil klepet.\n");
            client.Close();
        }

        static void Broadcast(string message) {
            Console.WriteLine(message);

            foreach (var client in clients.Values) {
                try {
                    NetworkStream stream = client.GetStream();

                    // ŠIFRIRANJE
                    byte[] buffer = Encoding.UTF8.GetBytes(Encrypt(message));
                    stream.Write(buffer, 0, buffer.Length);
                } catch { }
            }
        }
    }

    #region sifriranje
    public static string Encrypt(string message) {
        using (Aes aes = Aes.Create()) {
            aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
            aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
            ICryptoTransform encryptor = aes.CreateEncryptor();

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
    }
    public static string Decrypt(string encryptedMessage) {
        using (Aes aes = Aes.Create()) {
            aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
            aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
            ICryptoTransform decryptor = aes.CreateDecryptor();

            byte[] encryptedBytes = Convert.FromBase64String(encryptedMessage);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
    #endregion
}