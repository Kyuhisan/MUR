using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

class Program
{
    #region Konstante
    const int STD_PORT = 1234;
    const string STD_IP = "127.0.0.1";
    const int STD_MSG_SIZE = 1024;
    const string STD_CRYPT_KEY = "123456789012345678901234"; // Ključ (24 bajtni)
    const string STD_CRYPT_IV = "12345678"; // Inicializacijski vektor za TripleDES (8 bajtni)
    const string chessMoves = "qnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Placeholder
    #endregion

    #region komunikacija
    static string Receive(NetworkStream ns) // Metoda za prejem podatkovnega toka BLOKIRNA
    {
        try
        {
            byte[] recv = new byte[STD_MSG_SIZE]; // Buffer
            int length = ns.Read(recv, 0, recv.Length); // Branje podatkovnega toka
            return Encoding.UTF8.GetString(recv, 0, length); // Pretvorba iz byte v string
        }
        catch (Exception ex)
        {
            Console.WriteLine("Napaka pri prejemanju!\n" + ex.Message + "\n" + ex.StackTrace);
            return null;
        }
    }

    static void Send(NetworkStream ns, string message) // Metoda za pošiljanje podatkovnega toka in sporočila
    {
        try
        {
            byte[] send = Encoding.UTF8.GetBytes(message); // Pretvorba iz strign v byte
            ns.Write(send, 0, send.Length); // Pisanje v podatkovni tok
        }
        catch (Exception ex)
        {
            Console.WriteLine("Napaka pri pošiljanju!\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }
    #endregion

    #region sifriranje
    public static string Decrypt(string encryptedMessage) // Dešifriranje z TripleDES
    {
        using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider())
        {
            // Določitev šifrirnega ključa in inicializacijskega vektorja
            tdes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY);
            tdes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV);

            byte[] encryptedBytes = Convert.FromBase64String(encryptedMessage); // Pretvorba šifriranega sporočila iz Base64String v array šifriranih bajtov
            ICryptoTransform decryptor = tdes.CreateDecryptor(); // Dešifrant za dešifriranje

            // Dešifriranje sporočila v končno string sporočilo
            byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
    #endregion

    #region main
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8; // Za prikaz šumnikov
        Console.WriteLine("Odjemalec:");
        while (true)
        {
            Console.WriteLine("-----------\nVnesi ukaz:");
            string cmd = Console.ReadLine();

            if (cmd == "q")
            {
                break;
            }

            try
            {
                // Kreiranje TCP klienta in povezava z Connect() preko IP in PORTA (127.0.0.1 / 1234)
                TcpClient client = new TcpClient();
                client.Connect(STD_IP, STD_PORT);

                NetworkStream ns = client.GetStream(); // Pridobitev podatkovnega toka
                Send(ns, cmd); // Metoda za pošiljanje ukazov strežniku
                string response = Receive(ns); // Metoda za prejem odgovora s strežnika

                // Dešifriranje odgovora
                if (cmd[0] == 'm')
                {
                    response += " => " + Decrypt(response);
                }

                Console.WriteLine("--------------\nStrežnik vrača:\n" + response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Napaka: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
        Console.WriteLine("Pritisni poljubno tipko za zaključek programa.");
        Console.ReadKey();
    }
    #endregion
}
