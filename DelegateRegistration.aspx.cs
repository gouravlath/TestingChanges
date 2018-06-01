using SRAFDBConnection.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// Delegate registration class
    /// </summary>
    public partial class DelegateRegistration : System.Web.UI.Page
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Approval));
        //message for info to users
        protected string info = "", message_type = "", message_title = "",helpurl="";
        SQLServerConnection db = new SQLServerConnection();
        /// <summary>
        /// Page load event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["mail"] != null)
            {
                try
                {
                    helpurl = ConfigurationManager.AppSettings["srafUrl"].ToString().ToLower().Replace("automationprogress.aspx", "help.aspx?helpid=136").Replace("automation_progress.aspx", "help.aspx?helpid=136");
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
                DataTable dt = db.GetDelegateMaster(Session["mail"].ToString());
                ShowMessage();
                if (!IsPostBack)
                {
                    if (dt.Rows.Count > 0)
                    {
                        txtDelegatesEmail.Text = dt.Rows[0]["Delegates"].ToString();
                        txtStartDate.Text = Convert.ToDateTime(dt.Rows[0]["StartTime"]).Date.ToString("dd-MMM-yyyy");
                        txtEndDate.Text = Convert.ToDateTime(dt.Rows[0]["EndTime"]).Date.ToString("dd-MMM-yyyy");
                        ddlStartHour.SelectedValue = Convert.ToDateTime(dt.Rows[0]["StartTime"]).Hour.ToString().PadLeft(2, '0');
                        ddlStartMinutes.SelectedValue = Convert.ToDateTime(dt.Rows[0]["StartTime"]).Minute.ToString().PadLeft(2, '0');
                        ddlEndHours.SelectedValue = Convert.ToDateTime(dt.Rows[0]["EndTime"]).Hour.ToString().PadLeft(2, '0');
                        ddlEndMinutes.SelectedValue = Convert.ToDateTime(dt.Rows[0]["EndTime"]).Minute.ToString().PadLeft(2, '0');
                        if (bool.Parse(dt.Rows[0]["IsEnabled"].ToString()))
                        {
                            cbEnable.Checked = true;
                            txtMailBody.Text=String.Format("You have been nominated as a delegate for {0} from {1} to {2} ({3}).", Session["mail"].ToString(), Convert.ToDateTime(dt.Rows[0]["StartTime"]).ToString("dd-MMM-yyyy hh:mm tt"), Convert.ToDateTime(dt.Rows[0]["EndTime"]).ToString("dd-MMM-yyyy hh:mm tt"), Convert.ToDateTime(dt.Rows[0]["EndTime"]).ToString("dd-MMM-yyyy hh:mm tt"));
                            divMailBody.Attributes.Clear();
                            divMailBody.Attributes.Add("style", "display:block");
                        }
                        else
                        {
                            divMailBody.Attributes.Clear();
                            divMailBody.Attributes.Add("style", "display:none");
                        }
                    }
                    else
                    {
                        txtStartDate.Text = DateTime.Parse(db.GetCurrentDateTime()).Date.ToString("dd-MMM-yyyy");
                        txtEndDate.Text = DateTime.Parse(db.GetCurrentDateTime()).AddDays(1).Date.ToString("dd-MMM-yyyy");
                    }
                }
                else
                    ScriptManager.RegisterStartupScript(this, this.GetType(), "", "dateinit();", true);
            }
        }
        /// <summary>
        /// Save button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnSave_Click(object sender, EventArgs e)
        {
            DateTime StartTime = new DateTime();
            if (!Regex.IsMatch(txtDelegatesEmail.Text, "^[\\W]*([\\w+\\-.%]+@[\\w\\-.]+\\.[A-Za-z]{2,4}[\\W]*,{1}[\\W]*)*([\\w+\\-.%]+@[\\w\\-.]+\\.[A-Za-z]{2,4})[\\W]*$"))
            {
                Session["message"] = "Please enter valid comma seperated mailids of delegates";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if (txtDelegatesEmail.Text.Contains(Session["mail"].ToString()))
            {
                Session["message"] = "You can't nominate yourself as delegate";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if (txtStartDate.Text.Length.Equals(0))
            {
                Session["message"] = "Start Date Can't be empty.";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if (!DateTime.TryParse(txtStartDate.Text, out StartTime))
            {
                Session["message"] = "Enter a valid Start Date.";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if (!DateTime.TryParse(txtEndDate.Text, out StartTime))
            {
                Session["message"] = "Enter a valid End Date.";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if ((Convert.ToDateTime(txtEndDate.Text.ToString()) - (DateTime.Parse(db.GetCurrentDateTime()))).Days < 0)
            {
                Session["message"] = "End Date can't be before current date.";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            if ((Convert.ToDateTime(txtEndDate.Text.ToString()) - (Convert.ToDateTime(txtStartDate.Text.ToString()))).Days <= 0)
            {
                Session["message"] = "End Date Can't be before start date.";
                Session["message_title"] = "Error";
                Session["message_type"] = "Danger";
                ShowMessage();
                return;
            }
            StartTime=DateTime.Parse(txtStartDate.Text);
            StartTime = Convert.ToDateTime(DateTime.ParseExact(StartTime.Date.ToString("yyyy-MM-dd") + " " + ddlStartHour.SelectedValue + ":" + ddlStartMinutes.SelectedValue + ":00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            DateTime EndTime = DateTime.Parse(txtEndDate.Text);
            EndTime = Convert.ToDateTime(DateTime.ParseExact(EndTime.Date.ToString("yyyy-MM-dd") + " " + ddlEndHours.SelectedValue + ":" + ddlEndMinutes.SelectedValue + ":00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            if(db.UpdateDelegateMaster(Session["mail"].ToString(),txtDelegatesEmail.Text,StartTime,EndTime,cbEnable.Checked))
            {
                Session["message_title"] = "Success";
                if (cbEnable.Checked)
                    Session["message"] = "Successfully Enabled Delegation for Specified Period.";
                else
                    Session["message"] = "Successfully Disabled Delegation.";
                Session["message_type"] = "Success";
                ShowMessage();
                try
                {
                    if (cbEnable.Checked  && EndTime >= DateTime.Parse(db.GetCurrentDateTime()))
                    {
                         string messge = "<div style='font-family: Trebuchet MS; font-size: 10pt'><style>table {border-collapse: collapse;}table,th,td {border: 1px solid black;}</style><div>Dear Approver,<br><br>You have been nominated as a delegate for " + Session["mail"].ToString() + " from " + StartTime.ToString("dd-MMM-yyyy hh:mm tt") + " to " + EndTime.ToString("dd-MMM-yyyy hh:mm tt") + " (" + db.GetCurrentDBTimeZone() + ")"  +".</div>";
                        if(!string.IsNullOrEmpty(txtMailBody.Text))
                            messge = "<div style='font-family: Trebuchet MS; font-size: 10pt'><style>table {border-collapse: collapse;}table,th,td {border: 1px solid black;}</style><div>Dear Approver,<br><br>" + txtMailBody.Text.Replace("\n", "<br>") + ".</div>";
                        DataTable dt = db.getallConfigDetail();
                        foreach (var item in txtDelegatesEmail.Text.Split(',').ToList())
                        {
                            try
                            {
                                db.SendMail("Delegate Nomination", messge, item.Trim(), true, dt, MailPriority.High, Session["mail"].ToString(), "", "", "", "Delegate Nomination");
                            }
                            catch(Exception ex)
                            {
                                log.Error(String.Format("Error while sending delegate Nomination mail to {0}. {1}",item,ex.Message));
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    log.Error(String.Format("Error while sending delegate Nomination mail to delegates. {0}",ex.Message));
                }
            }
            else
            {
                Session["message_title"] = "Error Occured";
                Session["message"] = "An Internal Error Occured. Please try again later.";
                Session["message_type"] = "Danger";
                ShowMessage();
            }
            //Response.Redirect("~/DelegateRegistration.aspx");
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
        /// enable check event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void cbEnable_CheckedChanged(object sender, EventArgs e)
        {
            if (cbEnable.Checked)
            {
                divMailBody.Attributes.Clear();
                divMailBody.Attributes.Add("style", "display:block");
                MailBodyUpdate();
            }
            else
            {
                divMailBody.Attributes.Clear();
                divMailBody.Attributes.Add("style", "display:none");
            }
        }
        /// <summary>
        /// start date change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void txtStartDate_TextChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// start hour change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlStartHour_SelectedIndexChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// start minute change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlStartMinutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// end date change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void txtEndDate_TextChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// end hour change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlEndHours_SelectedIndexChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// end minute change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlEndMinutes_SelectedIndexChanged(object sender, EventArgs e)
        {
            MailBodyUpdate();
        }
        /// <summary>
        /// mail body update
        /// </summary>
        public void MailBodyUpdate()
        {
            DateTime StartTime = DateTime.Parse(txtStartDate.Text);
            StartTime = Convert.ToDateTime(DateTime.ParseExact(StartTime.Date.ToString("yyyy-MM-dd") + " " + ddlStartHour.SelectedValue + ":" + ddlStartMinutes.SelectedValue + ":00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            DateTime EndTime = DateTime.Parse(txtEndDate.Text);
            EndTime = Convert.ToDateTime(DateTime.ParseExact(EndTime.Date.ToString("yyyy-MM-dd") + " " + ddlEndHours.SelectedValue + ":" + ddlEndMinutes.SelectedValue + ":00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            txtMailBody.Text = "You have been nominated as a delegate for " + Session["mail"].ToString() + " from " + Convert.ToDateTime(StartTime).ToString("dd-MMM-yyyy hh:mm tt") + " to " + Convert.ToDateTime(EndTime).ToString("dd-MMM-yyyy hh:mm tt") + " (" + db.GetCurrentDBTimeZone() + ")" + ".";
        }
    }
}