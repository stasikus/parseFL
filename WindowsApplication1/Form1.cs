using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace WindowsApplication1
{
    public partial class Form1 : Form
    {
        public delegate void UpdateTextCallback(string message);
        public delegate void Action();
        Thread newThread;
        List<string> listOfUsers = new List<string>();
        List<string> nickName = new List<string>();
        List<string> category = new List<string>();
        List<string> mailList = new List<string>();
        List<string> phoneList = new List<string>();
        List<string> skypeList = new List<string>();
        List<string> ICQList = new List<string>();
        List<string> webList = new List<string>();
        string folder;
        string s = "Nick-Name;Category;Web-Pages;ICQ;Skype;Mail;Phone\n";


        public Form1()
        {
            InitializeComponent();
        }
        public static List<String> SearchAndInput(string str, string start, string end)
        {
            try
            {
                Regex rq = new Regex(start.Replace("[", "\\[").Replace("]", "\\]").Replace(".", "\\.").Replace("?", "\\?"));
                Regex rq1 = new Regex(end);
                List<string> ls = new List<string>();
                int p1 = 0;
                int p2 = 0;
                while (p1 < str.Length)
                {
                    Match m = rq.Match(str, p1);
                    if (m.Success)
                    {
                        p1 = m.Index + start.ToString().Length;
                        Match m1 = rq1.Match(str, p1);
                        if (m1.Success)
                        {
                            p2 = m1.Index;
                            if (str.Substring(p1, p2 - p1) == "")
                            {
                                ls.Add(str.Substring(p1, p2 - p1));
                            }
                            else ls.Add(str.Substring(p1, p2 - p1));
                        }
                    }
                    else break;
                }
                return ls;
            }
            catch
            {
                return null;
            }
        }



        private bool isDefic(string str)
        {
            if (str.IndexOf("class") != -1) return false;
            return true;

        }


        private void Form1_Load(object sender, EventArgs e)
        {
        }

       


private void people_button_Click(object sender, EventArgs e)
{
    newThread = new Thread(looking);
    newThread.IsBackground = true;
    newThread.Start();
}

void looking()
{
    MessageBox.Show("Start looking for ...");
    button1.BeginInvoke((Action)delegate
    {
        button1.Enabled = false;
    });
    people_button.BeginInvoke((Action)delegate
    {
        people_button.Enabled = false;
    });
    url.BeginInvoke((Action)delegate
    {
        url.Enabled = false;
    });
    try
    {
        category = new List<string>();
        listOfUsers = new List<string>();
        mailList = new List<string>();
        phoneList = new List<string>();
        skypeList = new List<string>();
        ICQList = new List<string>();
        webList = new List<string>();
        nickName = new List<string>();
        bool flag = true;
        int pageNum = 1;
        int counter = 0;
        Fenryr.Http.HttpClient httpClient = new Fenryr.Http.HttpClient();
        httpClient.ContentType = "application/x-www-form-urlencoded";
        httpClient.TextEncoding = Encoding.GetEncoding(1251);
        HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
        HtmlAgilityPack.HtmlDocument doc1 = new HtmlAgilityPack.HtmlDocument();

       
        while (flag == true)
        {
            string get = httpClient.Get(url.Text + "/?page=" + pageNum);
            doc.LoadHtml(get);

            var dataBlock = SearchAndInput(doc.DocumentNode.InnerHtml.Replace("\\", ""), "<span class=\"review-plus\"><a href=\"/users/", "/");

            /*----Category-----*/
            var categoryVal = SearchAndInput(doc.DocumentNode.InnerHtml, "<span class=\"cf-spec\">\r\n                                                ", "                                                <br>");
            for (int i = 0; i < categoryVal.Count; i++)
            {
                category.Add(categoryVal[i].Replace("Специализация: ", ""));
            }

            if (dataBlock.Count == 0)
            {
                flag = false;
                break;
            }

            for (int i = 0; i < dataBlock.Count; i += 2)
            {
                listOfUsers.Add(dataBlock[i]);
                nickName.Add(dataBlock[i]);
                get = httpClient.Get("https://www.fl.ru/users/" + dataBlock[i]);
                doc1.LoadHtml(get);

                /*----WEB-----*/
                var webPageSourse = doc1.DocumentNode.SelectSingleNode("//td[@class='ucHT']");
                if (webPageSourse == null)
                {
                    webList.Add("\"\"");
                }
                else
                {
                    List<string> webPage = SearchAndInput(webPageSourse.InnerHtml, "\">", "</a>");
                    webList.Add(webPage[0]);
                }

                /*----ICQ-----*/
                var ICQSourse = doc1.DocumentNode.SelectSingleNode("//td[@class='ucB']");
                if (ICQSourse == null)
                {
                    ICQList.Add("\"\"");
                }
                else
                {
                    List<string> ICQ = SearchAndInput(ICQSourse.InnerHtml, "\">\r\n            ", "                    </span>");
                    ICQList.Add(ICQ[0]);
                }

                /*----SKYPE-----*/
                var SkypeSourse = doc1.DocumentNode.SelectSingleNode("//td[@class='ucC']");
                if (SkypeSourse == null)
                {
                    skypeList.Add("\"\"");
                }
                else
                {
                    List<string> Skype = SearchAndInput(SkypeSourse.InnerHtml, "title=\"", "\">");
                    skypeList.Add(Skype[0]);
                }

                /*----MAIL-----*/
                var MailSourse = doc1.DocumentNode.SelectSingleNode("//td[@class='ucD']");
                if (MailSourse == null)
                {
                    mailList.Add("\"\"");
                }
                else
                {
                    List<string> Mail = SearchAndInput(MailSourse.InnerHtml, "\">", "</a>");
                    mailList.Add(Mail[0]);
                }

                /*----PHONE-----*/
                var PhoneSourse = doc1.DocumentNode.SelectSingleNode("//td[@class='ucA']");
                if (PhoneSourse == null)
                {
                    phoneList.Add("\"\"");
                }
                else
                {
                    List<string> Phone = SearchAndInput(PhoneSourse.InnerHtml, "<span>\r\n    ", "                    </span>\r\n");
                    phoneList.Add(Phone[0]);
                }
                found_num.BeginInvoke((Action)delegate
                {
                    found_num.Text = (counter+1).ToString();
                });


                s += nickName[counter] + ";" + category[counter] + ";" + webList[counter] + ";" + ICQList[counter] + ";" + skypeList[counter] + ";" + mailList[counter] + ";" + phoneList[counter] + ";";
                StreamWriter sw = new StreamWriter(@"" + folder + "\\Parse_people_list.csv", true, System.Text.Encoding.UTF8);
                sw.WriteLine(s);
                sw.Close();
                counter++;
                s = "";
                System.Threading.Thread.Sleep(500);
            }

            pageNum++;
        }

        MessageBox.Show("Done");

        button1.BeginInvoke((Action)delegate
        {
            button1.Enabled = true;
        });
        people_button.BeginInvoke((Action)delegate
        {
            people_button.Enabled = true;
        });
        url.BeginInvoke((Action)delegate
        {
            url.Enabled = true;
        });
    }
    catch (IOException er)
    {
        MessageBox.Show("Error to find people" + er);
    }
}



public void button1_Click(object sender, EventArgs e)
{
    using (FolderBrowserDialog dialog = new FolderBrowserDialog())
    {
        dialog.Description = "Choose your directory";
        dialog.ShowNewFolderButton = false;
        dialog.RootFolder = Environment.SpecialFolder.MyComputer;
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            folder = dialog.SelectedPath;
            if (folder != "")
                people_button.Enabled = true;
            else
                people_button.Enabled = false;
        }
        
    }
}

private void url_TextChanged(object sender, EventArgs e)
{
    if (url.TextLength > 20)
        button1.Enabled = true;
    else
        button1.Enabled = false;
}


        
    }
}