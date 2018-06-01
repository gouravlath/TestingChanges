using Newtonsoft.Json.Linq;
using SRAFDBConnection.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// Usecase catalogue page
    /// </summary>
    public partial class UsecaseCatalogue : System.Web.UI.Page
    {
        //init log
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Approval));
        //message for info to users
        protected string info = "", message_type = "", message_title = "";
        /// <summary>
        /// Page load event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if(!IsPostBack)
            {
                try
                {
                    string catid = string.Empty;
                    try
                    {
                        catid = Request.QueryString["catalogue"].ToString();
                    }
                    catch(Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                    SQLServerConnection obj = new SQLServerConnection();
                    DataTable dt = obj.getAllCreateRequestConfig();
                    if(dt!=null&&dt.Rows.Count>0)
                    {
                        if(catid=="")
                        {
                            DataRow[] arr = dt.Select("IsDefault=1");
                            if (arr.Count() > 0)
                            {
                                if(!string.IsNullOrEmpty(arr[0]["ConfigurationJson"].ToString()))
                                {
                                    hdnJson.Value = arr[0]["ConfigurationJson"].ToString();
                                    hdnName.Value = arr[0]["Name"].ToString();
                                }
                                else
                                {
                                    htmlContent.Visible = false;
                                    Session["message_title"] = "No catalogue found";
                                    Session["message"] = "No usecase catalogue found";
                                    Session["message_type"] = "Danger";
                                    ShowMessage();
                                }
                            }
                            else
                            {
                                htmlContent.Visible = false;
                                Session["message_title"] = "No catalogue found";
                                Session["message"] = "No usecase catalogue found";
                                Session["message_type"] = "Danger";
                                ShowMessage();
                            }
                        }
                        else
                        {
                            DataRow[] arr = dt.Select(String.Format("Name='{0}'"),catid);
                            if(arr.Count()>0)
                            {
                                if (!string.IsNullOrEmpty(arr[0]["ConfigurationJson"].ToString()))
                                {
                                    hdnJson.Value = arr[0]["ConfigurationJson"].ToString();
                                    hdnName.Value = arr[0]["Name"].ToString();
                                }
                                else
                                {
                                    htmlContent.Visible = false;
                                    Session["message_title"] = "No catalogue found";
                                    Session["message"] = "No usecase catalogue found";
                                    Session["message_type"] = "Danger";
                                    ShowMessage();
                                }
                            }
                            else
                            {
                                htmlContent.Visible = false;
                                Session["message_title"] = "Invalid catalogue name";
                                Session["message"] = "No catalogue found";
                                Session["message_type"] = "Danger";
                                ShowMessage();
                            }
                        }
                    }
                    else
                    {
                        htmlContent.Visible = false;
                        Session["message_title"] = "No catalogue configured";
                        Session["message"] = "No usecase catalogue found.";
                        Session["message_type"] = "Info";
                        ShowMessage();
                    }
                }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                catch(Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                {
                    htmlContent.Visible = false;
                    Session["message_title"] = "Error Occured";
                    Session["message"] = "Please try again later.";
                    Session["message_type"] = "Danger";
                    ShowMessage();
                }
            }
        }
        /// <summary>
        /// show message 
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
                log.Error(String.Format("error while getting message details.-{0}",ee.Message));
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
        /// Gets encrypted text
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [System.Web.Services.WebMethod]
        public static string GetEncryptedText(string id)
        {
            string encryptedText = Crypto.Encrypt(id);
            return encryptedText;
        }
    }
}