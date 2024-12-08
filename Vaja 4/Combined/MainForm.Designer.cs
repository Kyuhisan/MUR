using System.Windows.Forms;

namespace PeerToPeerApp
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Text = "Peer-to-Peer Communication";
            this.Width = 800;
            this.Height = 600;

            // Server Inputs
            Label serverIpLabel = new Label { Text = "Server IP:", Top = 10, Left = 10, Width = 80 };
            TextBox serverIpInput = new TextBox { Top = 10, Left = 90, Width = 150, Text = "127.0.0.1" };
            serverIpInput.Name = "ServerIPInput";

            Label serverPortLabel = new Label { Text = "Server Port:", Top = 10, Left = 260, Width = 80 };
            TextBox serverPortInput = new TextBox { Top = 10, Left = 340, Width = 100, Text = "25252" };
            serverPortInput.Name = "ServerPortInput";

            Button startServerButton = new Button { Text = "Start Server", Top = 10, Left = 460 };
            startServerButton.Click += (sender, e) =>
                StartServer(serverIpInput.Text, int.Parse(serverPortInput.Text));

            // Client Inputs
            Label clientIpLabel = new Label { Text = "Connect IP:", Top = 50, Left = 10, Width = 80 };
            TextBox clientIpInput = new TextBox { Top = 50, Left = 90, Width = 150, Text = "127.0.0.1" };
            clientIpInput.Name = "ClientIPInput";

            Label clientPortLabel = new Label { Text = "Connect Port:", Top = 50, Left = 260, Width = 80 };
            TextBox clientPortInput = new TextBox { Top = 50, Left = 340, Width = 100, Text = "25252" };
            clientPortInput.Name = "ClientPortInput";

            Button connectButton = new Button { Text = "Connect", Top = 50, Left = 460 };
            connectButton.Click += (sender, e) =>
                StartClient(clientIpInput.Text, int.Parse(clientPortInput.Text));

            // Messages and Input
            RichTextBox messagesBox = new RichTextBox { Top = 100, Left = 10, Width = 760, Height = 400, ReadOnly = true };
            messagesBox.Name = "MessagesBox";

            TextBox inputBox = new TextBox { Top = 520, Left = 10, Width = 660 };
            inputBox.Name = "InputBox";

            Button sendButton = new Button { Text = "Send", Top = 520, Left = 680 };
            sendButton.Click += SendMessage;

            // Add Controls
            this.Controls.Add(serverIpLabel);
            this.Controls.Add(serverIpInput);
            this.Controls.Add(serverPortLabel);
            this.Controls.Add(serverPortInput);
            this.Controls.Add(startServerButton);

            this.Controls.Add(clientIpLabel);
            this.Controls.Add(clientIpInput);
            this.Controls.Add(clientPortLabel);
            this.Controls.Add(clientPortInput);
            this.Controls.Add(connectButton);

            this.Controls.Add(messagesBox);
            this.Controls.Add(inputBox);
            this.Controls.Add(sendButton);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion
    }
}

