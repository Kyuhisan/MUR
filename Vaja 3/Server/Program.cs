using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Server
{
    internal class Program
    {
        static SortedList<string, TcpClient> clients = new SortedList<string, TcpClient>();

        #region Konstante
        const int STD_PORT = 1234;
        const string STD_IP = "127.0.0.1";
        const int STD_MSG_SIZE = 1024;
        const string STD_CRYPT_KEY = "123456789012345678901234";
        const string STD_CRYPT_IV = "12345678";
        static bool run = true;
        #endregion

        #region Komunikacija
        static string Receive(NetworkStream ns)
        {
            try
            {
                byte[] recv = new byte[STD_MSG_SIZE];
                int length = ns.Read(recv, 0, recv.Length);
                return Encoding.UTF8.GetString(recv, 0, length);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Napaka pri prejemanju!\n" + ex.Message + "\n" + ex.StackTrace);
                return null;
            }
        }

        static void Broadcast(string message)
        {
            Console.WriteLine(message);

            foreach (var client in clients.Values)
            {
                try
                {
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = Encoding.UTF8.GetBytes((message));
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch { }
            }
        }
        #endregion

        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse(STD_IP), STD_PORT);
            server.Start();
            Console.WriteLine("Strežnik teče na: " + STD_IP + ":" + STD_PORT);

            while (run)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(AcceptClient);
                clientThread.Start(client);
            }
        }

        static void AcceptClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            string username = "";

            try
            {
                username = Receive(stream);
                clients.Add(username, client);

                Broadcast(username + " se je pridružil klepetu.");
                ProcessMessages(username, stream, client);
            }
            catch
            {
                Console.WriteLine(username + " se je odjavil.");
            }
            finally
            {
                if (clients.ContainsKey(username))
                {
                    clients.Remove(username);
                }
                Broadcast(username + " je zapustil klepet.");
                client.Close();
            }
        }

        static void ProcessMessages(string username, NetworkStream stream, TcpClient client)
        {
            while (true)
            {
                // PROTOKOL
                string message = Receive(stream);
                string body;
                string head;

                if (message.Contains("|"))
                {
                    string[] parts = message.Split('|', (char)2);
                    head = parts.Length > 0 ? parts[0].ToUpper() : "";
                    body = parts.Length > 1 ? parts[1] : "";
                }
                else
                {
                    head = "B";
                    body = message;
                }

                switch (head)
                {
                    case "B": // BROADCAST
                        Broadcast(username + " pravi: " + (body));
                        break;
                    default:
                        Broadcast("Protokolna koda ni prepoznana.");
                        break;
                }
            }
        }

        #region Šifriranje
        public static string Encrypt(string message)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY.PadRight(16, ' '));
                aes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV.PadRight(16, ' '));
                ICryptoTransform encryptor = aes.CreateEncryptor();

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }
        public static string Decrypt(string encryptedMessage)
        {
            using (Aes aes = Aes.Create())
            {
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
}
