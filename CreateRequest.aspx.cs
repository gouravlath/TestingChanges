using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SRAFDBConnection.Utils;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using System.IO;
using System.Configuration;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// create request page
    /// </summary>
    public partial class CreateRequest : System.Web.UI.Page
    {
        //init log
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Approval));
        //message for info to users
        protected string info = "", message_type = "", message_title = "";
        /// <summary>
        /// page load event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            string ID = string.Empty;
            if(!IsPostBack)
            {
                ShowMessage();
            }
                try
                {
                    htmlContent.Visible = true;
                    ID = Crypto.Decrypt(Request.QueryString["id"].ToString());
                }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                catch(Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                {
                    if (Request.QueryString["id"] == null)
                    {
                        Response.Redirect("UsecaseCatalogue.aspx");
                    }
                    else
                    {
                        htmlContent.Visible = false;
                        Session["message_title"] = "Inavlid";
                        Session["message"] = "Form not available.";
                        Session["message_type"] = "Danger";
                        ShowMessage();
                    }
                }
            AddDynamicFields(ID);
        }
        /// <summary>
        /// shows message
        /// </summary>
        private void ShowMessage()
        {
            try
            {
                info = Session["message"].ToString();
                message_type = Session["message_type"].ToString();
                message_title = Session["message_title"].ToString();
            }
            catch (Exception ee)
            {
                log.Error(String.Format("error while getting message details.-{0}", ee.Message));
            }
            if (info.Length > 0)
            {
                message_div.Attributes.Clear();
                switch (message_type)
                {
                    case "Danger": message_div.Attributes.Add("class", "callout callout-danger"); break;
                    case "Warning": message_div.Attributes.Add("class", "callout callout-warning"); break;
                    case "Info": message_div.Attributes.Add("class", "callout callout-info"); break;
                    case "Success": message_div.Attributes.Add("class", "callout callout-success"); break;
                    default:
                        break;
                }
                message_div.Visible = true;
                Session["message"] = "";
                Session["message_title"] = "";
                Session["message_type"] = "";
            }
            else
            {
                message_div.Attributes.Add("display", "none");
            }
        }
        /// <summary>
        /// Adds dynamic fields
        /// </summary>
        /// <param name="Formid"></param>
        private void AddDynamicFields(string Formid)
        {
            if (!string.IsNullOrEmpty(Formid))
            {
                    fielddiv.Controls.Clear();
                    SQLServerConnection obj = new SQLServerConnection();
                    DataTable dt = obj.GetAllFormFields(Formid);
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            hdnFields.Value =String.Format("{0}{1},",hdnFields.Value,dr["fieldname"].ToString());
                        }
                        if (hdnFields.Value.Length > 0)
                        {
                            hdnFields.Value = hdnFields.Value.Substring(0, hdnFields.Value.Length - 1);
                        }
                    }
                    DataTable headerFooter = obj.GetFormheaderfooter(Formid);
                    lblFormname.Text = String.Format(" - {0}", headerFooter.Rows[0]["formname"].ToString());
                    if(headerFooter.Rows[0]["formtype"].ToString().ToLower()!="expert")
                    {
                    if (headerFooter != null && headerFooter.Rows.Count > 0 && headerFooter.Rows[0]["formheader"].ToString() != "")
                        fielddiv.Controls.Add(new LiteralControl(String.Format("<div class='form-group'>{0}</div>", ReplaceInputJsonValues(headerFooter.Rows[0]["formheader"].ToString()))));
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        foreach (DataRow dr in dt.Rows)
                        {
                            string type = dr["fieldtype"].ToString();
                            switch (type)
                            {
                                case "TEXTBOX":
                                    try
                                    {
                                        Label lbl = new Label();
                                        fielddiv.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        lbl.Text = !string.IsNullOrEmpty(dr["FieldQuestion"].ToString()) ? dr["FieldQuestion"].ToString() : dr["fieldname"].ToString();
                                        TextBox txt = new TextBox();
                                        txt.ID = String.Format("txt_{0}",dr["fieldid"].ToString());
                                        txt.CssClass = "form-control";
                                        fielddiv.Controls.Add(lbl);
                                        fielddiv.Controls.Add(new LiteralControl("</>"));
                                        fielddiv.Controls.Add(txt);
                                        fielddiv.Controls.Add(new LiteralControl("</div>"));
                                        if (Convert.ToBoolean(dr["isMandatory"].ToString()) == true)
                                            txt.Attributes.Add("required", "true");
                                        else
                                            txt.Attributes.Add("required", "false");
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(String.Format("error while creating field with id-{0}--{1}", dr["fieldid"].ToString(),ex.Message));
                                    }
                                    break;
                                case "DROPDOWN":
                                    try
                                    {
                                        Label lbl = new Label();
                                        fielddiv.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        lbl.Text = !string.IsNullOrEmpty(dr["FieldQuestion"].ToString()) ? dr["FieldQuestion"].ToString() : dr["fieldname"].ToString();
                                        DropDownList ddldrop = new DropDownList();
                                        ddldrop.ID = String.Format("ddl_{0}",dr["fieldid"].ToString());
                                        string[] values = ToStringArray(dr["fieldvalue"].ToString(), ',');
                                        foreach (string value in values)
                                        {
                                            ddldrop.Items.Add(value);
                                        }
                                        ddldrop.CssClass = "form-control";
                                        ddldrop.Items.Insert(0, new ListItem("Select"));
                                        fielddiv.Controls.Add(lbl);
                                        fielddiv.Controls.Add(ddldrop);
                                        fielddiv.Controls.Add(new LiteralControl("</div>"));
                                        if (Convert.ToBoolean(dr["isMandatory"].ToString()) == true)
                                            ddldrop.Attributes.Add("required", "true");
                                        else
                                            ddldrop.Attributes.Add("required", "false");
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(String.Format("error while creating field with id-{0}--{1}", dr["fieldid"].ToString(), ex.Message));
                                    }
                                    break;
                                case "CHECKBOXLIST":
                                    try
                                    {
                                        Label lbl = new Label();
                                        fielddiv.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        lbl.Text = !string.IsNullOrEmpty(dr["FieldQuestion"].ToString()) ? dr["FieldQuestion"].ToString() : dr["fieldname"].ToString();
                                        CheckBoxList ddldrop = new CheckBoxList();
                                        ddldrop.ID = String.Format("cbl_{0}",dr["fieldid"].ToString());
                                        string[] values = ToStringArray(dr["fieldvalue"].ToString(), ',');
                                        foreach (string value in values)
                                        {
                                            ddldrop.Items.Add(value);
                                        }
                                        ddldrop.CssClass = "";
                                        fielddiv.Controls.Add(lbl);
                                        fielddiv.Controls.Add(ddldrop);
                                        fielddiv.Controls.Add(new LiteralControl("</div>"));
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(String.Format("error while creating field with id-{0}--{1}", dr["fieldid"].ToString(), ex.Message));
                                    }
                                    break;
                                case "RADIO":
                                    try
                                    {
                                        Label lbl = new Label();
                                        fielddiv.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        lbl.Text = !string.IsNullOrEmpty(dr["FieldQuestion"].ToString()) ? dr["FieldQuestion"].ToString() : dr["fieldname"].ToString();
                                        RadioButtonList rdr = new RadioButtonList();
                                        rdr.ID = String.Format("radio_{0}",dr["fieldid"].ToString());                                    
                                        rdr.DataSource = dr["fieldvalue"].ToString().Split(',').ToList();
                                        rdr.SelectedValue = dr["fieldvalue"].ToString().Split(',').ToList()[0];
                                        rdr.DataBind();
                                        rdr.CssClass = "";
                                        fielddiv.Controls.Add(lbl);
                                        fielddiv.Controls.Add(rdr);
                                        fielddiv.Controls.Add(new LiteralControl("</div>"));
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(String.Format("error while creating field with id-{0}--{1}", dr["fieldid"].ToString(), ex.Message));
                                    }
                                    break;
                                case "DATE":
                                    try
                                    {
                                        Label lbl = new Label();
                                        fielddiv.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        lbl.Text = !string.IsNullOrEmpty(dr["FieldQuestion"].ToString()) ? dr["FieldQuestion"].ToString() : dr["fieldname"].ToString();
                                        TextBox txt = new TextBox();
                                        txt.ID = String.Format("txt_{0}",dr["fieldid"].ToString());
                                        txt.CssClass = "form-control datecontrol";
                                        fielddiv.Controls.Add(lbl);
                                        fielddiv.Controls.Add(new LiteralControl("</>"));
                                        fielddiv.Controls.Add(txt);
                                        fielddiv.Controls.Add(new LiteralControl("</div>"));
                                        if (Convert.ToBoolean(dr["isMandatory"].ToString()) == true)
                                            txt.Attributes.Add("required", "true");
                                        else
                                            txt.Attributes.Add("required", "false");
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(String.Format("error while creating field with id-{0}--{1}", dr["fieldid"].ToString(), ex.Message));
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (headerFooter != null && headerFooter.Rows.Count > 0 && headerFooter.Rows[0]["formfooter"].ToString() != "")
                            fielddiv.Controls.Add(new LiteralControl(String.Format("<div class='form-group'>{0}</div>", ReplaceInputJsonValues(headerFooter.Rows[0]["formfooter"].ToString()))));
                    }
                    }
                    else
                    {
                        fielddiv.Controls.Add(new LiteralControl(String.Format("<div >{0}</div>", headerFooter.Rows[0]["formhtml"].ToString())));
                    }
            }
        }
        /// <summary>
        /// Converts string to array
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        static string[] ToStringArray(string value, char separator)
        {
            return Array.ConvertAll(value.Split(separator), s => (s));
        }
        /// <summary>
        /// Create request click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCreaterequest_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", "openModal();", true);
        }
        /// <summary>
        /// Post method for aac api
        /// </summary>
        /// <param name="RESTApiURL"></param>
        /// <param name="POSTJson"></param>
        /// <param name="bearerkey"></param>
        /// <returns></returns>
        public static String POST(String RESTApiURL, String POSTJson, String bearerkey)
        {
            String status = "";
            var data = Encoding.ASCII.GetBytes(POSTJson);
            HttpWebRequest pRequest = (HttpWebRequest)WebRequest.Create(RESTApiURL);
            //Ignore HTTPS certificate errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            try
            {
                pRequest.Method = "POST";
                pRequest.ContentType = "application/json";
                pRequest.Accept = "application/json";
                pRequest.Timeout = 60000;
                if (bearerkey != "")
                    pRequest.Headers.Add(bearerkey);
                pRequest.ContentLength = data.Length;
                using (var stream = pRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                WebResponse response = null;
                StreamReader reader = null;
                response = (HttpWebResponse)pRequest.GetResponse();
                reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string pageResponseUpdate = reader.ReadToEnd();
                status = pageResponseUpdate;
            }
            catch (Exception ee)
            {
                status = String.Format("exception occured:--{0}",ee.Message);
            }
            return status;
        }
        /// <summary>
        /// get guid
        /// </summary>
        /// <returns></returns>
        public string Get8Digits()
        {
            var bytes = new byte[4];
            var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            uint random = BitConverter.ToUInt32(bytes, 0) % 100000000;
            return String.Format("{0:D8}", random);
        }
        /// <summary>
        /// Replaces json values
        /// </summary>
        /// <param name="InputJson"></param>
        /// <returns></returns>
        public string ReplaceInputJsonValues(string InputJson)
        {
            try
            {
                string JsonString = InputJson;
                string value = string.Empty;
                var lstMatches = Regex.Matches(JsonString, @"\$\$[^\$]*\$\$");
                foreach (var item in lstMatches)
                {
                    try
                    {
                        InputJson = InputJson.Replace(item.ToString(),"");
                        break;
                    }
                    catch (Exception ee)
                    {
                        log.Error(String.Format("error while parsing--{0}",ee.Message));
                    }
                }
            }
            catch (Exception ee)
            {
                log.Error(String.Format("error while parsing--{0}",ee.Message));
            }
            return InputJson.Trim();
        }
        /// <summary>
        /// Gets category
        /// </summary>
        /// <param name="formid"></param>
        /// <returns></returns>
        public String GetCategory(string formid)
        {
            String category = String.Empty;
            SQLServerConnection obj = new SQLServerConnection();
            category = obj.getCategoryName(formid);
            return category;
        }
        /// <summary>
        /// Confirm click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnConfirmStop_ServerClick(object sender, EventArgs e)
        {
            string formid = string.Empty;
            try
            {
                htmlContent.Visible = true;
                formid = Crypto.Decrypt(Request.QueryString["id"].ToString());
            }
            catch (Exception)
            {
                if (Request.QueryString["id"] == null)
                {
                    Response.Redirect("UsecaseCatalogue.aspx");
                }
                else
                {
                    htmlContent.Visible = false;
                    Session["message_title"] = "Inavlid";
                    Session["message"] = "Form not available.";
                    Session["message_type"] = "Danger";
                    ShowMessage();
                }
                return;
            }
            Ticket objTicket = new Ticket();
            objTicket.Category = GetCategory(formid);
            if (objTicket.Category.Trim().Length.Equals(0))
            {
                Session["message_title"] = "Error Occured";
                Session["message"] = "An Internal Error Occured. Please try again later.";
                Session["message_type"] = "Danger";
                ShowMessage();
                htmlContent.Visible = false;
                return;
            }
            objTicket.CreatedBy = Session["mail"].ToString();
            objTicket.ReturnAPI = "";
            objTicket.Summary = lblFormname.Text.Split('-')[1].Trim();
            objTicket.RequestID = "";
            Dictionary<String, String> list = new Dictionary<string, string>();
            var rjson = "";
            ContentPlaceHolder cph = (ContentPlaceHolder)this.Master.FindControl("form");
            SQLServerConnection obj = new SQLServerConnection();
            DataTable dt = obj.GetAllFormFields(formid);
            DataTable headerFooter = obj.GetFormheaderfooter(formid);
            if (headerFooter.Rows[0]["formtype"].ToString().ToLower() != "expert")
            {
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string type = row["fieldtype"].ToString();
                        switch (type)
                        {
                            case "TEXTBOX":
                            case "DATE":
                                try
                                {
                                    TextBox txt = cph.FindControl(String.Format("txt_{0}",row["fieldid"].ToString())) as TextBox;
                                    list.Add(row["fieldname"].ToString(), txt.Text);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                                break;
                            case "DROPDOWN":
                                try
                                {
                                    DropDownList ddl = cph.FindControl(String.Format("ddl_{0}",row["fieldid"].ToString())) as DropDownList;
                                    list.Add(row["fieldname"].ToString(), ddl.SelectedIndex != 0 ? ddl.SelectedItem.ToString() : null);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                                break;
                            case "CHECKBOXLIST":
                                try
                                {
                                    CheckBoxList cbl = cph.FindControl(String.Format("cbl_{0}",row["fieldid"].ToString())) as CheckBoxList;
                                    string value = "";
                                    if (cbl.Items != null && cbl.Items.Count > 0)
                                    {
                                        foreach (ListItem item in cbl.Items)
                                        {
                                            if (item.Selected == true)
                                                value = String.Format("{0}{1},",value,item.Value);
                                        }
                                    }
                                    list.Add(row["fieldname"].ToString(), value);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                                break;
                            case "RADIO":
                                try
                                {
                                    RadioButtonList rdr = cph.FindControl(String.Format("radio_{0}",row["fieldid"].ToString())) as RadioButtonList;                                    
                                    list.Add(row["fieldname"].ToString(), rdr.SelectedValue);
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (list.Count > 0)
                    rjson = new JavaScriptSerializer().Serialize(list);
            }
            else
            {
                if(!string.IsNullOrEmpty(hdnValue.Value))
                {
                    rjson = hdnValue.Value;
                }
            }
            try
            {
                if(rjson!=null&&rjson!="")
                {
                    JObject Output = JObject.Parse(rjson);
                    Output.Add("SessionUID", Session["uid"].ToString());
                    Output.Add("SessionName", Session["name"].ToString());
                    rjson = Output.ToString();
                }
                else
                {
                    JObject Output = new JObject();
                    Output.Add("SessionUID", Session["uid"].ToString());
                    Output.Add("SessionName", Session["name"].ToString());
                    rjson = Output.ToString();
                }
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
            }
            objTicket.Parameters = rjson;
            var Rjson = new JavaScriptSerializer().Serialize(objTicket);
            string SRAFAPI = ConfigurationManager.AppSettings["SRAFAPI"].ToString();
            String RESULT = POST(SRAFAPI, Rjson, String.Format("Bearer:{0}",obj.getBearerKeyName(formid)));
            log.Info(String.Format("Result from sraf api-{0}",RESULT));
            if (!RESULT.ToLower().Contains("exception occured"))
            {
                ApprovalWorkflowApp.Utils.Crypto.ResponseMessage objResult = new JavaScriptSerializer().Deserialize<ApprovalWorkflowApp.Utils.Crypto.ResponseMessage>(RESULT);
                if (objResult.MessageType == "Success")
                {
                    Session["message_title"] = "Success";
                    Session["message"] = String.Format("{0} Your RequestID is - {1}",objResult.Message,objResult.RequestID);
                    Session["message_type"] = "Success";
                    if (Request.QueryString["id"] == null)
                        Response.Redirect("~/UsecaseCatalogue.aspx");
                    else
                        Response.Redirect(String.Format("~/CreateRequest.aspx?id={0}",Request.QueryString["id"]));
                }
                else
                {
                    Session["message_title"] = "Failure";
                    Session["message"] = objResult.Message;
                    Session["message_type"] = "Danger";
                    ShowMessage();
                    return;
                }
            }
            else
            {
                Session["message_title"] = "Error Occured";
                Session["message"] = "An Internal Error Occured. Please try again later.";
                Session["message_type"] = "Danger";
                ShowMessage();
            }
        }
    }
}