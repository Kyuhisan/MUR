using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PeerToPeerApp
{
    public partial class MainForm : Form
    {
        private Thread serverThread;
        private Thread clientThread;
        private TcpListener server;
        private TcpClient client;
        private TcpClient connectedClient;
        private NetworkStream serverStream;
        private NetworkStream clientStream;
        private bool run = true;

        private bool isServerConnected = false; // True when a client connects to this server
        private bool isClientConnected = false; // True when this instance connects to another server
        private bool isDualConnected = false;  // True when both are connected as server and client

        private byte[] symmetricKey; // Shared symmetric key

        private ECDiffieHellmanCng dh; // Diffie-Hellman instance
        private byte[] publicKey; // Public key for this instance

        private const int STD_MSG_SIZE = 1024;

        public MainForm()
        {
            InitializeComponent();

            // Initialize Diffie-Hellman for key exchange
            dh = new ECDiffieHellmanCng();
            dh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            dh.HashAlgorithm = CngAlgorithm.Sha256;
            publicKey = dh.PublicKey.ToByteArray();
        }

        private void StartServer(string ip, int port)
        {
            serverThread = new Thread(() => RunServer(ip, port)) { IsBackground = true };
            serverThread.Start();
            UpdateTitle();
            UpdateMessages($"Server started on {ip}:{port}...");
        }

        private void RunServer(string ip, int port)
        {
            try
            {
                server = new TcpListener(IPAddress.Parse(ip), port);
                server.Start();
                connectedClient = server.AcceptTcpClient();
                serverStream = connectedClient.GetStream();
                isServerConnected = true;
                UpdateTitle();
                UpdateMessages("Client connected to the server.");

                // Receive public key from client
                byte[] clientPublicKey = ReceiveRaw(serverStream);
                UpdateMessages($"Received client's public key: {BitConverter.ToString(clientPublicKey).Replace("-", "")}");

                // Send public key to client
                SendRaw(serverStream, publicKey);
                UpdateMessages($"Sent public key to client: {BitConverter.ToString(publicKey).Replace("-", "")}");

                // Generate symmetric key
                symmetricKey = dh.DeriveKeyMaterial(CngKey.Import(clientPublicKey, CngKeyBlobFormat.EccPublicBlob));
                UpdateMessages($"Generated symmetric key: {BitConverter.ToString(symmetricKey).Replace("-", "")}");

                CheckDualConnection();

                while (run)
                {
                    string message = ReceiveEncrypted(serverStream);
                    if (message != null)
                    {
                        Invoke(new Action(() =>
                        {
                            UpdateMessages($"Client: {message}");
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMessages($"Server error: {ex.Message}");
            }
        }

        private void StartClient(string ip, int port)
        {
            clientThread = new Thread(() => RunClient(ip, port)) { IsBackground = true };
            clientThread.Start();
            UpdateTitle();
            UpdateMessages($"Attempting to connect to server at {ip}:{port}...");
        }

        private void RunClient(string ip, int port)
        {
            try
            {
                client = new TcpClient(ip, port);
                clientStream = client.GetStream();
                isClientConnected = true;
                UpdateTitle();
                UpdateMessages("Connected to the server.");

                // Send public key to server
                SendRaw(clientStream, publicKey);
                UpdateMessages($"Sent public key to server: {BitConverter.ToString(publicKey).Replace("-", "")}");

                // Receive public key from server
                byte[] serverPublicKey = ReceiveRaw(clientStream);
                UpdateMessages($"Received server's public key: {BitConverter.ToString(serverPublicKey).Replace("-", "")}");

                // Generate symmetric key
                symmetricKey = dh.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));
                UpdateMessages($"Generated symmetric key: {BitConverter.ToString(symmetricKey).Replace("-", "")}");

                CheckDualConnection();

                while (run)
                {
                    string message = ReceiveEncrypted(clientStream);
                    if (message != null)
                    {
                        Invoke(new Action(() =>
                        {
                            UpdateMessages($"Server: {message}");
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateMessages($"Client error: {ex.Message}");
            }
        }

        private void UpdateTitle()
        {
            if (isServerConnected && isClientConnected)
            {
                this.Text = "Peer-to-Peer Communication - Server & Client";
            }
            else if (isServerConnected)
            {
                this.Text = "Peer-to-Peer Communication - Server";
            }
            else if (isClientConnected)
            {
                this.Text = "Peer-to-Peer Communication - Client";
            }
            else
            {
                this.Text = "Peer-to-Peer Communication";
            }
        }

        private void SendMessage(object sender, EventArgs e)
        {
            TextBox inputBox = (TextBox)this.Controls["InputBox"];
            string message = inputBox.Text.Trim();
            inputBox.Clear();

            try
            {
                if (isDualConnected)
                {
                    // Two-way communication: both client and server can send messages
                    if (serverStream != null && connectedClient != null)
                    {
                        SendEncrypted(serverStream, message);
                        UpdateMessages($"Server: {message}");
                    }
                    if (clientStream != null)
                    {
                        SendEncrypted(clientStream, message);
                        UpdateMessages($"Client: {message}");
                    }
                }
                else if (isClientConnected)
                {
                    // One-way communication: client can send messages to the server
                    if (clientStream != null)
                    {
                        SendEncrypted(clientStream, message);
                        UpdateMessages($"Client: {message}");
                    }
                    else
                    {
                        UpdateMessages("Cannot send message: No active connection as a client.");
                    }
                }
                else
                {
                    UpdateMessages("Cannot send message: Two-way communication not established.");
                }
            }
            catch (Exception ex)
            {
                UpdateMessages($"Error sending message: {ex.Message}");
            }
        }



        private void SendRaw(NetworkStream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        private byte[] ReceiveRaw(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            return data;
        }

        private void SendEncrypted(NetworkStream stream, string message)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = symmetricKey;
                aes.GenerateIV();
                byte[] iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    byte[] plaintextBytes = Encoding.UTF8.GetBytes(message);
                    byte[] ciphertext = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                    // Send IV followed by ciphertext
                    SendRaw(stream, iv);
                    SendRaw(stream, ciphertext);
                }
            }
        }

        private string ReceiveEncrypted(NetworkStream stream)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = symmetricKey;

                // Read IV
                byte[] iv = ReceiveRaw(stream);
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    // Read ciphertext
                    byte[] ciphertext = ReceiveRaw(stream);
                    byte[] plaintextBytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                    return Encoding.UTF8.GetString(plaintextBytes);
                }
            }
        }

        private void CheckDualConnection()
        {
            if (isServerConnected && isClientConnected && !isDualConnected)
            {
                isDualConnected = true;
                UpdateMessages("Two-way communication established.");
            }
        }

        private void UpdateMessages(string message)
        {
            RichTextBox messagesBox = (RichTextBox)this.Controls["MessagesBox"];
            messagesBox.AppendText(message + Environment.NewLine);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            run = false;
            server?.Stop();
            client?.Close();
            connectedClient?.Close();
            base.OnFormClosing(e);
        }
    }
}
