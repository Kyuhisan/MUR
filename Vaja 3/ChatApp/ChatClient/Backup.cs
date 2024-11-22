//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading;
//using System.Windows.Forms;

//namespace ChatClient {
//    public partial class Client : Form {
//        #region Konstante
//        const int STD_PORT = 1234;
//        const string STD_IP = "127.0.0.1";
//        const int STD_MSG_SIZE = 1024;
//        const string STD_CRYPT_KEY = "123456789012345678901234";
//        const string STD_CRYPT_IV = "12345678";
//        #endregion

//        private TcpClient? client;
//        private NetworkStream? stream;
//        private Thread? receiveThread;
//        bool isRunning = true;

//        public Client() {
//            InitializeComponent();
//        }

//        private void Send(object sender, EventArgs e) {
//            try {
//                string message = MessageTextBox.Text.Trim();

//                // ŠIFRIRANJE
//                byte[] sendBuffer = Encoding.UTF8.GetBytes(Encrypt(message));
//                stream.Write(sendBuffer, 0, sendBuffer.Length);
//                MessageTextBox.Clear();

//            } catch (Exception ex) {
//                Console.WriteLine("Napaka pri pošiljanju!\n" + ex.Message + "\n" + ex.StackTrace);
//            }
//        }


//        private void Receive() {
//            try {
//                while (isRunning) {
//                    byte[] buffer = new byte[STD_MSG_SIZE];
//                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

//                    if (bytesRead == 0) break;

//                    // DEŠIFRIRANJE
//                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//                    Invoke((MethodInvoker)(() => ChatTextBox.AppendText(Decrypt(message) + Environment.NewLine)));
//                }
//            } catch {
//                if (isRunning) {
//                    Invoke((MethodInvoker)(() => ChatTextBox.AppendText("Povezava prekinjena." + Environment.NewLine)));
//                }
//            }
//        }

//        private void Connect(object sender, EventArgs e) {
//            try {
//                client = new TcpClient(STD_IP, STD_PORT);
//                stream = client.GetStream();

//                byte[] buffer = Encoding.UTF8.GetBytes(UsernameTextBox.Text.Trim());
//                stream.Write(buffer, 0, buffer.Length);

//                receiveThread = new Thread(Receive);
//                receiveThread.Start();

//                ChatTextBox.AppendText("Priključen na klepet." + Environment.NewLine);
//                ConnectButton.Enabled = false;
//            } catch (Exception ex) {
//                MessageBox.Show($"Napaka pri povezavi: {ex.Message}");
//            }
//        }

//        #region sifriranje
//        public static string Encrypt(string message) {
//            using (Aes aes = Aes.Create()) {
//                aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
//                aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
//                ICryptoTransform encryptor = aes.CreateEncryptor();

//                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
//                byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
//                return Convert.ToBase64String(encryptedBytes);
//            }
//        }
//        #endregion

//        #region desifriranje
//        public static string Decrypt(string encryptedMessage) {
//            using (Aes aes = Aes.Create()) {
//                aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
//                aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
//                ICryptoTransform decryptor = aes.CreateDecryptor();

//                byte[] encryptedBytes = Convert.FromBase64String(encryptedMessage);
//                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
//                return Encoding.UTF8.GetString(decryptedBytes);
//            }
//        }
//        #endregion

//        private void CloseWindow(object sender, FormClosingEventArgs e) {
//            try {
//                isRunning = false;
//                stream?.Close();
//                client?.Close();
//            } catch (Exception ex) {
//                MessageBox.Show($"Napaka pri zapiranju: {ex.Message}");
//            }
//        }
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.Net;
//using System.Net.Sockets;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading;

//class ChatServer {
//    #region Konstante
//    const int STD_PORT = 1234;
//    const string STD_IP = "127.0.0.1";
//    const int STD_MSG_SIZE = 1024;
//    const string STD_CRYPT_KEY = "123456789012345678901234";
//    const string STD_CRYPT_IV = "12345678";
//    #endregion


//    static Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
//    static object lockObj = new object();

//    static void Main(string[] args) {
//        TcpListener server = new TcpListener(IPAddress.Parse(STD_IP), STD_PORT);
//        server.Start();
//        Console.WriteLine($"Strežnik teče na {STD_IP}:{STD_PORT}");

//        while (true) {
//            TcpClient client = server.AcceptTcpClient();
//            Thread clientThread = new Thread(HandleClient);
//            clientThread.Start(client);
//        }
//    }

//    static void HandleClient(object obj) {
//        TcpClient client = (TcpClient)obj;
//        NetworkStream stream = client.GetStream();
//        string username = "";

//        try {
//            byte[] buffer = new byte[1024];
//            int bytesRead = stream.Read(buffer, 0, buffer.Length);
//            username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

//            lock (lockObj) {
//                clients.Add(username, client);
//            }

//            Broadcast($"\n* {username} se je pridružil klepetu.\n");

//            while (true) {
//                buffer = new byte[STD_MSG_SIZE];
//                bytesRead = stream.Read(buffer, 0, buffer.Length);

//                if (bytesRead == 0) {
//                    break;
//                }

//                // DEŠIFRIRANJE
//                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
//                Console.WriteLine(message);
//                Broadcast($"{username} pravi: {Decrypt(message)}");
//            }
//        } catch {
//            Console.WriteLine($"{username} se je odjavil.");
//        } finally {
//            lock (lockObj) {
//                if (clients.ContainsKey(username)) {
//                    clients.Remove(username);
//                }
//                Broadcast($"* {username} je zapustil klepet.\n");
//                client.Close();
//            }
//        }

//        static void Broadcast(string message) {
//            Console.WriteLine(message);

//            lock (lockObj) {
//                foreach (var client in clients.Values) {
//                    try {
//                        NetworkStream stream = client.GetStream();

//                        // ŠIFRIRANJE
//                        byte[] buffer = Encoding.UTF8.GetBytes(Encrypt(message));
//                        stream.Write(buffer, 0, buffer.Length);
//                    } catch { }
//                }
//            }
//        }
//    }

//    #region sifriranje
//    public static string Encrypt(string message) {
//        using (Aes aes = Aes.Create()) {
//            aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
//            aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
//            ICryptoTransform encryptor = aes.CreateEncryptor();

//            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
//            byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
//            return Convert.ToBase64String(encryptedBytes);
//        }
//    }
//    #endregion

//    #region desifriranje
//    public static string Decrypt(string encryptedMessage) {
//        using (Aes aes = Aes.Create()) {
//            aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
//            aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
//            ICryptoTransform decryptor = aes.CreateDecryptor();

//            byte[] encryptedBytes = Convert.FromBase64String(encryptedMessage);
//            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
//            return Encoding.UTF8.GetString(decryptedBytes);
//        }
//    }
//    #endregion
//}