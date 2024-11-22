using System;
using System.IO;
using System.Net;
using System.Net.Sockets; //socket(), bind()
using System.Security.Cryptography;
using System.Text;

class Program {
    #region Konstante
    const int STD_PORT = 1234;
    const string STD_IP = "127.0.0.1";
    const int STD_MSG_SIZE = 1024;
    const string STD_CRYPT_KEY = "123456789012345678901234"; // Ključ (24 bajtni)
    const string STD_CRYPT_IV = "12345678"; // Inicializacijski vektor za TripleDES (8 bajtni)
    const int STD_HEAD_LEN = 1; // Dolzina sporočila, ki predstavlja ukaz in ne vsebino
    static bool run = true;
    #endregion

    #region komunikacija
    static string Receive(NetworkStream ns) // Metoda za prejem podatkovnega toka BLOKIRNA
    {
        try {
            byte[] recv = new byte[STD_MSG_SIZE]; // Buffer
            int length = ns.Read(recv, 0, recv.Length); // Branje podatkovnega toka
            return Encoding.UTF8.GetString(recv, 0, length); // Pretvorba iz byte v string
        } catch (Exception ex) {
            Console.WriteLine("Napaka pri prejemanju!\n" + ex.Message + "\n" + ex.StackTrace);
            return null;
        }
    }

    static void Send(NetworkStream ns, string message) // Metoda za pošiljanje podatkovnega toka in sporočila
    {
        try {
            byte[] send = Encoding.UTF8.GetBytes(message); // Pretvorba iz strign v byte
            ns.Write(send, 0, send.Length); // Pisanje v podatkovni tok
        } catch (Exception ex) {
            Console.WriteLine("Napaka pri pošiljanju!\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }
    #endregion

    #region sifriranje
    public static string Encrypt(string message) { // Šifriranje z TripleDES
        using (TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider()) {
            // Določitev šifrirnega ključa in inicializacijskega vektorja
            tdes.Key = Encoding.UTF8.GetBytes(STD_CRYPT_KEY);
            tdes.IV = Encoding.UTF8.GetBytes(STD_CRYPT_IV);

            byte[] messageBytes = Encoding.UTF8.GetBytes(message); // Pretvorba šifriranega sporočila iz Base64String v array šifriranih bajtov
            ICryptoTransform encryptor = tdes.CreateEncryptor(); // Šifrant za šifriranje

            // Šifriranje sporočila v končno šifrirano sporočilo
            byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }
    }
    #endregion

    #region sahovnica
    static string PrintChesssahovnica(string fen) {
        try {
            string[] figure = fen.Split(' ');
            if (figure.Length != 6) {
                throw new FormatException("Vnešena notacija ni veljavna. Ponovno vnesi sporočilo, ki vsebuje 6 polj.");
            }

            string figura = figure[0];
            string trenutnaRunda = figure[1];
            string moznostRokade = figure[2];
            string moznostEnPassant = figure[3];
            int polPoteza = int.Parse(figure[4]);
            int trenutnaPoteza = int.Parse(figure[5]);

            char[,] sahovnica = new char[8, 8];
            string[] vrsticas = figura.Split('/');

            for (int i = 0; i < 8; i++) {
                string vrstica = vrsticas[i];
                int stolpec = 0;

                foreach (char c in vrstica) {
                    if (char.IsDigit(c)) {
                        stolpec += (int)char.GetNumericValue(c);
                    } else if ("prnbqkPRNBQK".IndexOf(c) != -1) {
                        sahovnica[i, stolpec] = c;
                        stolpec++;
                    } else {
                        throw new FormatException("Sporočilo vsebuje neveljaven znak: " + c);
                    }
                }
            }

            StringBuilder output = new StringBuilder();
            output.AppendLine("Šahovnica:\n  a b c d e f g h");
            output.AppendLine(" +----------------+");

            for (int i = 0; i < 8; i++) {
                output.Append(i + 1 + "|");
                for (int j = 0; j < 8; j++) {
                    char piece = sahovnica[i, j];
                    output.Append((piece == '\0' ? '.' : piece) + " ");
                }
                output.AppendLine("|");
            }

            output.AppendLine(" +----------------+");
            output.AppendLine("Na Potezi: " + (trenutnaRunda == "w" ? "Beli" : "Črni") + "\n");
            output.AppendLine("Možnosti Rokade: " + Rokada(moznostRokade) + "\n");
            output.AppendLine("Možnosti En Passant: " + moznostEnPassant + "\n");
            output.AppendLine("Število Polpotez: " + polPoteza + "\n");
            output.AppendLine("Število Potez: " + trenutnaPoteza + "\n");

            return output.ToString();
        } catch (FormatException ex) {
            return "Napaka notacije: " + ex.Message;
        } catch (Exception ex) {
            return "Napaka notacije: " + ex.Message;
        }
    }

    static string Rokada(string moznostRokade) {
        if (string.IsNullOrEmpty(moznostRokade) || moznostRokade == "-") {
            return "Brez Možnosti Rokade";
        }
        string pravila = "";

        if (moznostRokade.Contains('K')) {
            pravila += "\nBeli, Kraljeva Stran"; 
        }
        if (moznostRokade.Contains('Q')) {
            pravila += "\nBeli, Kraljičina Stran"; 
        }
        if (moznostRokade.Contains('k')) {
            pravila += "\nČrni, Kraljeva Stran"; 
        }
        if (moznostRokade.Contains('q')) {
            pravila += "\nČrni, Kraljičina Stran";
        }

        if (pravila.EndsWith(", ")) {
            pravila = pravila.Substring(0, pravila.Length - 2);
        }

        return pravila;
    }
    #endregion

    #region main
    private static void Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        // Ustvarimo TCP listener, ki poslusa na IP in PORT (127.0.0.1 / 1234) in tega zazenemo
        TcpListener listener = new TcpListener(IPAddress.Parse(STD_IP), STD_PORT);
        listener.Start(); // (listen)

        Console.WriteLine("Strežnik:\n---------\nPosluša na IP naslovu: " + STD_IP + ":" + STD_PORT.ToString());

        while (run) { // Zanka, ki neprestano teče na strežniku
            using (TcpClient client = listener.AcceptTcpClient()) // Accept() za sprejem vzpostavitve povezave z klientom BLOKIRNA METODA
            using (NetworkStream ns = client.GetStream()) {
                Console.WriteLine("Odjemalec se je povezal (" + client.Client.RemoteEndPoint.ToString() + ").");
                // Receive() za sprejetje sporočila od klienta BLOKIRNA METODA
                string message = Receive(ns);
                Console.WriteLine("Dobil sem sporočilo: " + message);
                string response = "";

                #region Protocol
                string head = message[0].ToString().ToLower();
                string body = "";

                // Ločitev na vsebino in ukaz
                if (message.Length > 1) {
                    body = message.Substring(STD_HEAD_LEN, message.Length - 1);
                }

                switch (head) {
                    case "g": // Pozdravljen IP:PORT
                        response = "Pozdravljen [" + client.Client.RemoteEndPoint.ToString() + "]";
                        break;
                    case "h": // Datum & Čas
                        response = "Današnji datum in čas je: " + DateTime.Now.ToString();
                        break;
                    case "i": // Trenutni Direktorij
                        response = "Nahajate se v direktoriju: " + Directory.GetCurrentDirectory().ToString();
                        break;
                    case "j": // Prejeto Sporočilo
                        response = message.Substring(1);
                        break;
                    case "k": // Sistemske Informacije
                        response = "Ime naprave: " + Environment.MachineName + ", Verzija OS: " + Environment.OSVersion.ToString();
                        break;
                    case "l": // Forsyth-Edwards notacija
                        response = PrintChesssahovnica(message.Substring(1));
                        break;
                    case "m": // TripleDES šifriranje
                        response = Encrypt(message.Substring(1));
                        break;
                    default:
                        response = "Strežnik ni prepoznal ukaza protokola!";
                        break;
                }
                #endregion

                // Posredovanje odgovora klientu z Send()
                Send(ns, response);
                Console.WriteLine("Odgovoril sem: " + response);
            }
            Console.WriteLine("Odjemalec se je odklopil.\n");
        }
    }
    #endregion
}