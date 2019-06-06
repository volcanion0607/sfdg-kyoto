using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.ServiceModel;
using salesforce_csharp_soap_sample.sforce;
using System.Net;

namespace salesforce_csharp_soap_sample
{
    public partial class MainForm : Form
    {

        private static SoapClient loginClient;
        private static SoapClient client;
        private static SessionHeader header;
        private static EndpointAddress endpoint;

        public MainForm()
        {
            InitializeComponent();
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            buttonLogin.Enabled = true;
            buttonLogout.Enabled = false;
            buttonGetContacts.Enabled = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void ButtonLogin_Click(object sender, EventArgs e)
        {
            if (login())
            {
                MessageBox.Show("Login Successful!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonLogin.Enabled = false;
                buttonLogout.Enabled = true;
                buttonGetContacts.Enabled = true;
            }
            else
            {
                buttonLogin.Enabled = true;
                buttonLogout.Enabled = false;
                buttonGetContacts.Enabled = false;
            }
        }

        private void ButtonGetContacts_Click(object sender, EventArgs e)
        {
            getContacts();
        }

        private void ButtonLogout_Click(object sender, EventArgs e)
        {
            if (logout())
            {
                MessageBox.Show("Logged out.", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                buttonGetContacts.Enabled = false;
                buttonLogout.Enabled = false;
                buttonLogin.Enabled = true;
            }
            else
            {
                buttonGetContacts.Enabled = true;
                buttonLogout.Enabled = true;
                buttonLogin.Enabled = false;
            }
        }

        private bool login()
        {
            string username = textUserName.Text;
            string password = textPassword.Text;

            loginClient = new SoapClient();
            LoginResult loginResult;
            try
            {
                loginResult = loginClient.login(null, username, password);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (loginResult.passwordExpired)
            {
                MessageBox.Show("パスワードの有効期限が切れています。", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            endpoint = new EndpointAddress(loginResult.serverUrl);

            header = new SessionHeader();
            header.sessionId = loginResult.sessionId;

            client = new SoapClient("Soap", endpoint);

            //showUserInfo(loginResult, loginResult.serverUrl);

            return true;
        }

        private void showUserInfo(LoginResult lr, String authEP)
        {
            try
            {
                GetUserInfoResult userInfo = lr.userInfo;
                String userId = userInfo.userId;
                String userFullName = userInfo.userFullName;
                String userEmail = userInfo.userEmail;
                String serverUrl = lr.serverUrl;

                String userInfoText = $@"UserID:
{userId}
User Full Name:
{userFullName}
User Email:
{userEmail}
Auth End Point:
{authEP}
Service End Point:
{serverUrl}
";
                MessageBox.Show(userInfoText, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void getContacts()
        {
            String soqlQuery = "SELECT FirstName, LastName, Email, Phone FROM Contact";
            try
            {
                QueryResult qr;
                LimitInfo[] limitInfos =
                    client.query(
                    header,
                    null,
                    null,
                    null,
                    soqlQuery,
                    out qr
                    );

                bool done = false;

                if (qr.size > 0)
                {
                    List<MyContact> contacts = new List<MyContact>();
                    while (!done)
                    {
                        sObject[] records = qr.records;
                        for (int i = 0; i<records.Length; i++)
                        {
                            Contact contact = (Contact)records[i];
                            MyContact myContact = new MyContact();
                            myContact.FirstName = contact.FirstName;
                            myContact.LastName = contact.LastName;
                            myContact.Email = contact.Email;
                            myContact.Phone = contact.Phone;
                            contacts.Add(myContact);
                        }

                        if (qr.done)
                        {
                            done = true;
                        }
                        else
                        {
                            limitInfos =
                                client.queryMore(
                                header,
                                null,
                                qr.queryLocator,
                                out qr
                                );
                        }
                    }

                    dataGridViewContacts.DataSource = contacts;

                }
                else
                {
                    MessageBox.Show("条件に一致するレコードが存在しません。", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool logout()
        {
            try
            {
                client.logout(header);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

    }

    public class MyContact
    {
        public String FirstName
        {
            set; get;
        }
        public String LastName
        {
            set; get;
        }
        public String Email
        {
            set; get;
        }
        public String Phone
        {
            set; get;
        }
    }
}
