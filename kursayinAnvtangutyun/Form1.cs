using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace kursayinAnvtangutyun
{
    public partial class Form1 : Form
    {
        private IMqttClient mqttClient;
        string randomString;
        public Form1()
        {
            InitializeComponent();
            InitializeMqttClient();
            Random random = new Random();

            // Generate a random number with 6 digits
            int randomNumber = random.Next(100000, 999999); // Range is [100000, 999999)

            // Convert the number to a string
            randomString = randomNumber.ToString();

            Console.WriteLine("Random number with 6 digits: " + randomString);
            // Add encryption types to the ComboBox
            comboBox1.Items.AddRange(new string[] { "None", "AES", "TripleDES", "RC2", "SHA256" });
            comboBox1.SelectedIndex = 0; // Set default encryption type to None

            // Add example keys and IVs to the ComboBoxes
            comboBox2.Items.AddRange(new string[] {
    "abcdefghijklmnop",
    "12345678901234567890123456789012",
    "abcdefghabcdefgh",
    "98765432109876543210987654321098",
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
});
            comboBox2.SelectedIndex = 0;
            comboBox3.Items.AddRange(new string[] {
    "1234567890123456",
    "abcdefgh12345678",
    "87654321abcdefgh",
    "9876543210abcdef",
    "fedcba0987654321"
});
            comboBox3.SelectedIndex = 0;
        }

        private async void InitializeMqttClient()
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("postman.cloudmqtt.com", 13045) // Replace with your MQTT server address and port
                .WithCredentials("tdytoggi", "P5CzG7kJ4RZh") // Replace with your username and password
                .Build();

            mqttClient.UseConnectedHandler(async e =>
            {
                // Subscribe to the desired topic when connected
                await mqttClient.SubscribeAsync("topic/with/messages");
            });

            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                // Parse the received message and update the UI
                string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                string decryptedMessage = DecryptMessage(message); // Decrypt the received message
                AddItemToListBox(decryptedMessage);
            });

            await mqttClient.ConnectAsync(options);
        }

        private void AddItemToListBox(string item)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke((MethodInvoker)(() => listBox1.Items.Add(item)));
            }
            else
            {
                listBox1.Items.Add(item);
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // Get the message from the TextBox
            string message = textBox1.Text;

            // Get the selected encryption type
            string selectedEncryptionType = comboBox1.SelectedItem?.ToString();

            // Get the selected key and IV
            string selectedKey = comboBox2.SelectedItem?.ToString();
            string selectedIV = comboBox3.SelectedItem?.ToString();

            // Encrypt the message based on the selected encryption type and key/IV
            string encryptedMessage = EncryptMessage(message, selectedEncryptionType, selectedKey, selectedIV);

            // Publish the encrypted message to the MQTT server
            await mqttClient.PublishAsync("topic/with/messages", Encoding.UTF8.GetBytes("ID:"+randomString+" "+ encryptedMessage));

            // Clear the TextBox
            textBox1.Text = string.Empty;
        }

        private string EncryptMessage(string message, string encryptionType, string key, string iv)
        {
            if (encryptionType == "None")
            {
                return message; // No encryption
            }
            else if (encryptionType == "AES")
            {
                return EncryptAES(message, key, iv);
            }
            else if (encryptionType == "TripleDES")
            {
                return EncryptTripleDES(message, key, iv);
            }
            else if (encryptionType == "RC2")
            {
                return EncryptRC2(message, key, iv);
            }
            else if (encryptionType == "SHA256")
            {
                return ComputeSHA256(message);
            }
            else
            {
                throw new ArgumentException("Unknown encryption type");
            }
        }
        private string GetSelectedEncryptionType()
        {
            if (comboBox1.InvokeRequired)
            {
                return (string)comboBox1.Invoke((Func<string>)(() => comboBox1.SelectedItem?.ToString()));
            }
            else
            {
                return comboBox1.SelectedItem?.ToString();
            }
        }
        private string GetSelectedKey()
        {
            if (comboBox2.InvokeRequired)
            {
                return (string)comboBox2.Invoke((Func<string>)(() => comboBox2.SelectedItem?.ToString()));
            }
            else
            {
                return comboBox2.SelectedItem?.ToString();
            }
        }
        private string GetSelectedIv()
        {
            if (comboBox3.InvokeRequired)
            {
                return (string)comboBox3.Invoke((Func<string>)(() => comboBox3.SelectedItem?.ToString()));
            }
            else
            {
                return comboBox3.SelectedItem?.ToString();
            }
        }
        private string DecryptMessage(string encryptedMessage)
        {
            string selectedEncryptionType = GetSelectedEncryptionType();
            string selectedKey = GetSelectedKey();
            string selectedIV = GetSelectedIv();

            if (selectedEncryptionType == "None")
            {
                return encryptedMessage; // No decryption needed
            }
            else if (selectedEncryptionType == "AES")
            {
                return DecryptAES(encryptedMessage, selectedKey, selectedIV);
            }
            else if (selectedEncryptionType == "TripleDES")
            {
                return DecryptTripleDES(encryptedMessage, selectedKey, selectedIV);
            }
            else if (selectedEncryptionType == "RC2")
            {
                return DecryptRC2(encryptedMessage, selectedKey, selectedIV);
            }
            else if (selectedEncryptionType == "SHA256")
            {
                return encryptedMessage;
            }
            else
            {
                throw new ArgumentException("Unknown encryption type");
            }
        }

        private string EncryptAES(string message, string key, string iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptAES(string encryptedMessage, string key, string iv)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(key);
                aesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedMessage)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private string EncryptTripleDES(string message, string key, string iv)
        {
            using (TripleDESCryptoServiceProvider tripleDesAlg = new TripleDESCryptoServiceProvider())
            {
                tripleDesAlg.Key = Encoding.UTF8.GetBytes(key);
                tripleDesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform encryptor = tripleDesAlg.CreateEncryptor(tripleDesAlg.Key, tripleDesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptTripleDES(string encryptedMessage, string key, string iv)
        {
            using (TripleDESCryptoServiceProvider tripleDesAlg = new TripleDESCryptoServiceProvider())
            {
                tripleDesAlg.Key = Encoding.UTF8.GetBytes(key);
                tripleDesAlg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = tripleDesAlg.CreateDecryptor(tripleDesAlg.Key, tripleDesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedMessage)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private string EncryptRC2(string message, string key, string iv)
        {
            using (RC2CryptoServiceProvider rc2Alg = new RC2CryptoServiceProvider())
            {
                rc2Alg.Key = Encoding.UTF8.GetBytes(key);

                ICryptoTransform encryptor = rc2Alg.CreateEncryptor(rc2Alg.Key, rc2Alg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(message);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        private string DecryptRC2(string encryptedMessage, string key, string iv)
        {
            using (RC2CryptoServiceProvider rc2Alg = new RC2CryptoServiceProvider())
            {
                rc2Alg.Key = Encoding.UTF8.GetBytes(key);
                rc2Alg.IV = Encoding.UTF8.GetBytes(iv);

                ICryptoTransform decryptor = rc2Alg.CreateDecryptor(rc2Alg.Key, rc2Alg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedMessage)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        private string ComputeSHA256(string message)
        {
            using (SHA256 sha256Alg = SHA256.Create())
            {
                byte[] hashBytes = sha256Alg.ComputeHash(Encoding.UTF8.GetBytes(message));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
