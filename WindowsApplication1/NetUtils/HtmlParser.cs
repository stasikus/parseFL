using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using Fenryr.IO;


namespace Fenryr.Http
{

    public  delegate bool HtmlElementGetter (HtmlElement el);

   

    public class HtmlElementList : List<HtmlElement>
    {
        public HtmlElement this[string name]
        {
            get
            {
                foreach (HtmlElement el in this)
                    if (el.Name == name) return el;
                return null;
            }
        }

        public HtmlElement GetById(string id)
         {
            foreach (HtmlElement el in this)
            {
                string _id = el.GetAttr("id");
                if (_id.ToUpper() == id.ToUpper()) return el;
            }
                   return null;
        }

        public HtmlElementList SelectElements(HtmlElementGetter elementGetter)
        {
            HtmlElementList list = new HtmlElementList();
            
            foreach (HtmlElement el in this)
            {
                if (elementGetter(el)) list.Add(el);
            }
            return list;

        }

        internal HtmlElementList()
        {

        }
    }

   public class HtmlElement 
   {
       string _name ;
       string _src;
       string _tagname;
       string innerText;


       HtmlElementList _children = new HtmlElementList();
       public HtmlElementList Children { get { return _children; } }
       HtmlElement _owner = null;
       public HtmlElement Owner { get { return _owner; } }

       public string InnerText
       {
           get
           {
               if (innerText != null) return innerText;          
                   Regex reg = new Regex(">([^<]+)");
                   innerText = System.Web.HttpUtility.HtmlDecode(reg.Match(InnerHtml).Groups[1].Value);
                   return innerText;
           }
       }

       public HtmlElementList
           GetElementsByTagName(string tagname)
       {
           HtmlElementList res = new HtmlElementList();
           Regex reg = new Regex(@"(?<=<\s*?"+tagname+@")(?<fval>[\s\S]*?)(?=<\s*?/\s*?"+tagname+@"\s*?>)", RegexOptions.IgnoreCase |  RegexOptions.Singleline);
           MatchCollection mc = reg.Matches(_src);
           if (mc.Count == 0) mc = new Regex(@"(?<=<\s*?" + tagname + @")(?<fval>[\s\S]*?)(?=\s*?/{0,1}>)", RegexOptions.IgnoreCase |  RegexOptions.Singleline).Matches(_src);
           foreach (Match m in mc)
           {
               res.Add(new HtmlElement(m.Groups["fval"].Value, tagname));
           }

           return res;
       }

        public HtmlElementList
           GetElementsByTagsName(string [] tags)
       {
           HtmlElementList res = new HtmlElementList();
           foreach (string tag in tags)
           {
               HtmlElementList tagList = GetElementsByTagName(tag);
               foreach (HtmlElement el in tagList)
               {
                   res.Add(el);
               }
           }
           return res;
       }

       public HtmlElementList GetDataElements()
       {
           return GetElementsByTagsName(new string[] { "input", "select", "textarea"});
       }

       public HtmlElement(string src, string tagname)
       {
           _src = src;
           _tagname = tagname.ToUpper();
       }

       public HtmlElement(string src)
       {
           _src = src;
           _tagname = new Regex(@"<\s*?(\w+)").Match(src).Groups[1].Value.ToUpper();
       }

       internal HtmlElement(HtmlElement owner)
       {
           _owner = owner;
       }

       public bool Checked
       {
           get
           {
               return (Tagname.ToLower() == "input" && InnerHtml.ToLower().Contains("checked"));
           }
       }

       public bool Selected
       {
           get
           {
               return (Tagname.ToLower() == "option" && InnerHtml.ToLower().Contains("selected"));
           }
       }

       public string Tagname 
       {
           get 
           {
               return _tagname;
           }
       }

       public string GetAttr2(string attname)
       {
           Regex regex = new Regex(attname + @"\s*?\=\s*?[""]([^"">]+)[""]", RegexOptions.IgnoreCase);
           Match match = regex.Match(_src);
           if (!match.Success)
           {
               regex = new Regex(attname + @"\s*?\=\s*?([^\s'"">]+)", RegexOptions.IgnoreCase);
               match = regex.Match(_src);
           }
           return ((match.Success) ? System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value) : String.Empty);
       }

       public string GetAttr (string attname)
       {
           Regex regex = new Regex(attname + @"\s*?\=\s*?['""]([^'"">]+)['""]", RegexOptions.IgnoreCase);
           Match match = regex.Match(_src);
           if (!match.Success)
           {
               regex = new Regex(attname + @"\s*?\=\s*?([^\s'"">]+)", RegexOptions.IgnoreCase);
               match = regex.Match(_src);
           }
           return ((match.Success) ? System.Web.HttpUtility.HtmlDecode(match.Groups[1].Value) : String.Empty);
       }

       public string Name
       {
           get
           {
               if (!String.IsNullOrEmpty(_name)) return _name;
               _name = GetAttr("name");
               return _name;
           }
       }
       public string InnerHtml
       {
           get {return _src;}
       }
   }
    
    class HtmlForm : HtmlElement
    {

        string _action;

        public HtmlForm (string src) : base (src , "FORM")
        {
        }

        public string Action
        {
            get
            {
                if (!String.IsNullOrEmpty(_action)) return _action;
                _action = GetAttr("action");
               // if (!_action.StartsWith("http") && !_action.StartsWith("/")) _action = "/" + _action;
                return _action;
            }
        }

        public string BuildAction(string Url)
        {
            if (Uri.IsWellFormedUriString(Action, UriKind.Absolute)) return Action;

            if (Uri.IsWellFormedUriString(Url, UriKind.Absolute))
            {
                string sHost = new Uri(Url).Authority;
                return ((Url.Contains("https") ? "https://" : "http://") +sHost + Action);
            }
            return "http://" + Url + Action;
        }


        Dictionary<string, string> _formParams = null;
        Dictionary<string, string> _formFiles = null;



       MultiPartFormDataStream GetFormStream(Encoding encoding)
        {
            MultiPartFormDataStream ms = new MultiPartFormDataStream();
			ms.TextEncoding = encoding;
            foreach (KeyValuePair<string, string> pair in this.FormParams)
            {
                ms.AddFormField(pair.Key, pair.Value);
            }
            foreach (KeyValuePair<string, string> pair in this.FormFiles)
            {
                try
                {
                    ms.AddFile(pair.Key, pair.Value, "application/octet-stream");
                }
                catch { }
            }
          //  ms.SetEndOfData();
            return ms;
        }

     public   String GetFormUrlencoded(Encoding encoding)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;

            foreach (KeyValuePair<string, string> pair in FormParams)
            {
                if (first)
                {
                    first = false;
                    sb.Append(System.Web.HttpUtility.UrlEncode(pair.Key, encoding) + "=" +
                               System.Web.HttpUtility.UrlEncode(pair.Value, encoding));
                }
                else sb.Append("&" + System.Web.HttpUtility.UrlEncode(pair.Key, encoding) + "=" +
                     System.Web.HttpUtility.UrlEncode(pair.Value, encoding));
            }

            return sb.ToString();
        }


       public System.IO.Stream GetPostStream(ref string ContentType)
        {
            return GetPostStream(ref ContentType , Encoding.UTF8);
        }

       public System.IO.Stream GetPostStream(ref string ContentType , Encoding encoding)
        {
            if (GetAttr("enctype") != null && GetAttr("enctype").ToLower() == "multipart/form-data")
            {
                MultiPartFormDataStream ms = GetFormStream(encoding);
                ContentType = "multipart/form-data; boundary=" + ms.Boundary;
                return ms;
           }
            string sPost = GetFormUrlencoded(encoding);
            ContentType = "application/x-www-form-urlencoded";
            return new MemoryStream(encoding.GetBytes(sPost));
           
        }

        Random rand = new Random();

        Dictionary<string, string> GetFormParams()
        {
            HtmlElementList all = GetDataElements();
            Dictionary<string, string> res = new Dictionary<string, string>();
            foreach (HtmlElement el in all)
            {
               if (string.IsNullOrEmpty(el.Name)) continue;
                if (el.Tagname.ToLower() == "input")
                {
                    string _type = el.GetAttr("type").ToLower();
                    if (_type == "submit" && el.Name == "") continue; 
                    if (_type == "text" || _type == "hidden" || _type == "submit" || _type == "button" || _type == "password")
                    {
                        res[el.Name] = el.GetAttr("value");
                    }
                    else if (_type == "image")
                    {
                        res[el.Name + ".x"] = rand.Next(5, 10).ToString();
                        res[el.Name + ".y"] = rand.Next(5, 10).ToString();
                    }
                    else if (_type == "radio" || _type == "checkbox")
                    {
                        if (el.Checked)
                        {
                            if (!String.IsNullOrEmpty(el.GetAttr("value")))
                                res[el.Name] = el.GetAttr("value");
                            else res[el.Name] = "1";
                        }
                    }                
                }
                else if (el.Tagname.ToLower() == "textarea")
                {
                    res[el.Name] = el.InnerText;
                }
                else if (el.Tagname.ToLower() == "select")
                {
                    HtmlElementList options = el.GetElementsByTagName("option");
                    foreach (HtmlElement option in options)
                    {
                        if (option.Selected)
                        {
                            string value = option.GetAttr("value");
                            if (String.IsNullOrEmpty(value))
                                   value = option.InnerText;
                               res[el.Name] = value; 
                            break;
                        }
                    }
                    if (!res.ContainsKey(el.Name) && options.Count > 0)
                    {
                        string value = options[0].GetAttr("value");
                        if (String.IsNullOrEmpty(value))
                            value = options[0].InnerText;
                        res[el.Name] = value; 
                    }
                }
            }
            return res;
        }

        Dictionary<string, string> GetFormFiles()
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            HtmlElementList inputs = GetElementsByTagName("input");
            foreach (HtmlElement el in inputs)
            {
                    string _type = el.GetAttr("type");
                    if (_type == "file" )
                    {
                        res[el.Name] = "";
                    }                   
            }
            return res;
        }

       public Dictionary<string, string> FormParams
        {
            get
            {
                if (_formParams == null) _formParams = GetFormParams();
                return _formParams;
            }
        }

       public Dictionary<string, string> FormFiles
        {
            get
            {
                if (_formFiles == null) _formFiles = GetFormFiles();
                return _formFiles;
            }
        }


       
        
    }
    
    
    
    class HtmlParser 
    {
        HtmlElementList _forms;
        string _htmlString;

        public HtmlElement ToHtmlElement
        {
            get
            {
                return new HtmlElement(_htmlString, "HTML");
            }
        }

        string ClearHtml (string Html)
        {
            return Regex.Replace(Html,@"<!--[\s\S]*?-->", "");
        }

        public HtmlParser(string html)
        {
            _htmlString = ClearHtml ( html );
        }

        void FindForms()
        {
            MatchCollection mc = Regex.Matches(_htmlString, @"(?<=<\s*?form)(?<fval>[\s\S]*?)(?=<\s*?/\s*?form\s*?>)", RegexOptions.IgnoreCase );
            _forms = new HtmlElementList();
            foreach (Match m in mc)
            {
                _forms.Add(new HtmlForm(m.Groups["fval"].Value));
            }
        }


        public HtmlElementList  Forms
        {
            get
            {
                if (_forms == null) FindForms();
                return _forms;
            }
        }

       

        public static HtmlForm FindForm(string sHtml, string elTag, string elName, string elType, string elValue)
        {
            return null;
            /*
            HtmlParser parser = new HtmlParser(sHtml);
            HtmlForm[] forms = parser.Forms.Where 
            (
              el =>
              {
                  HtmlForm f = el as HtmlForm;
                  if (f == null) return false;
                  HtmlElement e = f.GetElementsByTagName(elTag)[elName];
                  if (e == null) return false;
                  return (e.GetAttr("type") == elType && e.GetAttr("value") == elValue);
              }
            ).ToArray();
            return (forms.Length > 0) ? forms[0] : null;
             */
        }
       
    }
}
