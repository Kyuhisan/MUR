using System;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Client {
    public partial class ClientForm : Form {
        TcpClient client;
        NetworkStream stream;
        Thread receiveThread;

        public ClientForm() {
            InitializeComponent();
        }

        private void ConnectButtonClick(object sender, EventArgs e) {
            try {
                client = new TcpClient("127.0.0.1", 1234);
                stream = client.GetStream();

                byte[] buffer = Encoding.UTF8.GetBytes(UsernameTextBox.Text.Trim());
                stream.Write(buffer, 0, buffer.Length);

                receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                ChatTextBox.AppendText("Priključen na klepet...\n");
                ConnectButtonClick().Enabled = false;
            } catch (Exception ex) {
                MessageBox.Show($"Napaka pri povezavi: {ex.Message}");
            }
        }

        private void SendButton_Click(object sender, EventArgs e) {
            try {
                string message = MessageTextBox.Text.Trim();
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                stream.Write(buffer, 0, buffer.Length);

                MessageTextBox.Clear();
            } catch (Exception ex) {
                MessageBox.Show($"Napaka pri pošiljanju: {ex.Message}");
            }
        }

        private void ReceiveMessages() {
            try {
                while (true) {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0) {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Invoke((MethodInvoker)(() => ChatTextBox.AppendText(message + "\n")));
                }
            } catch (Exception ex) {
                Invoke((MethodInvoker)(() => ChatTextBox.AppendText("Povezava prekinjena.\n")));
            }
        }

        private void ChatClientForm_FormClosing(object sender, FormClosingEventArgs e) {
            try {
                stream?.Close();
                client?.Close();
                receiveThread?.Abort();
            } catch { }
        }
    }
}
