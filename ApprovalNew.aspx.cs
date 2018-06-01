using ApprovalWorkflowApp.Utils;
using CustomConfigurationManager;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SRAFDBConnection.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// Approval page
    /// </summary>
    public partial class ApprovalNew : System.Web.UI.Page
    {
        //init log
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(ApprovalNew));
        //message for info to users
        protected string info = "", message_type = "", message_title = "", countPending = "", countAction = "", RequestUrl = "", countNotes = "";
        protected KeyValuePair<string, string> list;
        bool IsApproved = false;
        SQLServerConnection obj = new SQLServerConnection();
        string GroupName = string.Empty;
        string approversList = string.Empty;
        bool isapprover = false;
        string MailType = string.Empty;
        /// <summary>
        /// page load event for approval app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                bool isAuthorized = false;
                bool isExceptionalApprover = false;
                GetPendingActions();
                log.Debug("Trying to get sys id from url");
                string sys = string.Empty;
                try
                {
                    sys = Request.QueryString["sys"] != null ? ApprovalWorkflowApp.Utils.Crypto.Decrypt(HttpUtility.UrlDecode(Request.QueryString["sys"])) : null;
                }
                catch (Exception ex)
                {
                    log.Error("Error while decoding sys id--" + ex.Message);
                }
                try
                {
                    if (Request.QueryString["mode"] != null && Request.QueryString["mode"].ToString() == "exist" && !IsPostBack)
                    {
                        InactivateAll();
                        existingFormLI.Attributes.Add("class", "active");
                        existingFormLI.Attributes.Add("class", "tab-pane active");
                        existingForm.Attributes.Add("class", "active");
                        existingForm.Attributes.Add("class", "tab-pane active");
                    }                    
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
                if (sys == null || sys == "")
                {
                    log.Info("sys id is null or empty");
                    Session["message_title"] = "Info";
                    Session["message"] = "Invalid request details";
                    Session["message_type"] = "Info";
                    ShowMessage();
                    automationLi.Visible = false;
                    ApprovalLi.Visible = false;
                    MailLi.Visible = false;
                    existingFormLI.Visible = false;
                    return;
                }
                else
                {
                    log.Info("successfully got the sys id--" + sys);
                    Session["sys"] = sys;
                }
                DataTable dt = null;
                try
                {
                    dt = obj.getRequestData(Session["sys"].ToString());
                }
                catch (Exception ex)
                {
                    log.Error("error while getting request data--" + ex.Message);
                }
                if (dt.Rows.Count.Equals(0))
                {
                    Session["message_title"] = "No inputs needed";
                    Session["message"] = "Looks like one of the approver has given all the inputs that we needed. Incase we need any further information / approval, we’ll send you an email.";
                    Session["message_type"] = "Info";
                    ShowMessage();
                    automationLi.Visible = false;
                    ApprovalLi.Visible = false;
                    MailLi.Visible = false;
                    existingFormLI.Visible = false;
                    return;
                }
                dt.Rows[0]["description"] = HttpUtility.HtmlDecode(dt.Rows[0]["description"].ToString());
                Session["requestid"] = dt.Rows[0]["requestid"].ToString();
                if (dt.Rows[0]["description"].ToString().StartsWith("{"))
                {
                    var desc = JsonConvert.DeserializeObject(dt.Rows[0]["description"].ToString());
                    desc = SyntaxHighlightJson(JsonConvert.SerializeObject(desc, Formatting.Indented));
                    dt.Rows[0]["description"] = "<pre>" + desc + "</pre>";
                }
                string status = dt.Rows[0]["status"].ToString();
                int formid = 0;
                string requestid = string.Empty;
                DataTable objtable = null;
                log.Info("status for sysid--" + status);
                log.Debug("trying to load form based on status for sysid-" + Session["sys"].ToString());
                isapprover = false;
                Session["programid"] = dt.Rows[0]["programid"].ToString();
                GetApprovalList();
                GetMailHistory();
                GetComments();
                try
                {
                    isAuthorized = CheckAuthorization();
                    try
                    {
                        isExceptionalApprover = Session["isExceptionalApprover"] != null ? (Convert.ToBoolean(Session["isExceptionalApprover"])) : false;
                    }
                    catch (Exception)
                    {
                        isExceptionalApprover = false;
                    }
                }
                catch (Exception)
                {
                    isAuthorized = false;
                    isExceptionalApprover = false;
                }
                bool isl2 = false;
                bool isuser = false;
                string request_type = string.Empty;
                string tickettype = string.Empty;
                requestid = dt.Rows[0]["requestid"].ToString();
                Session["l2Approver"] = dt.Rows[0]["l2approver"].ToString();
                try
                {
                    RequestUrl = ConfigurationManager.AppSettings["srafUrl"].ToString() + "?id=" + ApprovalWorkflowApp.Utils.Crypto.Encrypt(requestid) + "&pid=" + ApprovalWorkflowApp.Utils.Crypto.Encrypt(Session["programid"].ToString());
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
                switch (status)
                {
                    case "SENTFORL2":
                        log.Info("inside the SENTFORL2 switch case");
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        log.Info("formid for sysid--" + Session["sys"].ToString() + " is--" + formid);
                        try
                        {
                            log.Debug("trying to compare session mailid with l2 approver in db");
                            IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                            isl2 = isapprover;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                            isl2 = false;
                        }
                        if (isl2 == true)
                        {
                            existingFormLI.Visible = true;
                            ApprovalLi.Visible = false;
                            MailLi.Visible = false;
                            string formtype = dt.Rows[0]["formtype"].ToString().ToLower();
                            objtable = obj.GetFormFields(formid.ToString());
                            if (objtable != null && objtable.Rows.Count > 0)
                            {
                                foreach (DataRow dr in objtable.Rows)
                                {
                                    hdnFields.Value = hdnFields.Value + dr["fieldname"].ToString() + ",";
                                }
                                if (hdnFields.Value.Length > 0)
                                {
                                    hdnFields.Value = hdnFields.Value.Substring(0, hdnFields.Value.Length - 1);
                                }
                            }
                            if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                            {
                                log.Info(Session["mail"].ToString() + " is a valid l2 approver");
                                try
                                {
                                    log.Debug("trying to get all form fields for formid-" + formid);
                                    log.Info("successfully got all info for form fields for formid--" + formid);
                                }
                                catch (Exception ex)
                                {
                                    log.Error("error while getting all form fields for formid--" + ex.Message);
                                    if (ex.InnerException != null)
                                    {
                                        log.Error(ex.InnerException.Message);
                                    }
                                }
                                request_type = dt.Rows[0]["ProgramType"].ToString();
                                tickettype = dt.Rows[0]["RequestType"].ToString();
                                newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                else
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                if (tickettype.Contains("TASK"))
                                {
                                    DataTable questiondatatable = new DataTable();
                                    questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                    if (questiondatatable.Rows.Count > 0)
                                    {
                                        newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                        foreach (DataRow dr in questiondatatable.Rows)
                                        {
                                            newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                        }
                                        newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                    }
                                }
                                newForm.Controls.Add(new LiteralControl("</table>"));
                                newForm.Controls.Add(new LiteralControl("</div>"));
                                if (dt.Rows[0]["formheader"].ToString() != null && dt.Rows[0]["formheader"].ToString() != "")
                                {
                                    string header = ReplaceInputJsonValues(dt.Rows[0]["formheader"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + header + "</div>"));
                                }
                                log.Debug("trying to create l2 form for formid--" + formid);
                                foreach (DataRow row in objtable.Rows)
                                {
                                    Label lbl = new Label();
                                    lbl.Font.Bold = true;
                                    newForm.Controls.Add(new LiteralControl("<br/>"));
                                    string type = row["fieldtype"].ToString();
                                    switch (type)
                                    {
                                        case "TEXTBOX":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                TextBox txt = new TextBox();
                                                txt.ID = "txt_" + row["fieldid"].ToString();                                                
                                                txt.CssClass = "form-control";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(txt);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "DROPDOWN":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                DropDownList ddldrop = new DropDownList();
                                                ddldrop.ID = "ddl_" + row["fieldid"].ToString();
                                                string[] values = ToStringArray(row["fieldvalue"].ToString(), ',');
                                                foreach (string value in values)
                                                {
                                                    ddldrop.Items.Add(value);
                                                }
                                                ddldrop.CssClass = "form-control";
                                                ddldrop.Items.Insert(0, new ListItem("Select"));
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(ddldrop);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "CHECKBOXLIST":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                CheckBoxList ddldrop = new CheckBoxList();
                                                ddldrop.ID = "cbl_" + row["fieldid"].ToString();
                                                string[] values = ToStringArray(row["fieldvalue"].ToString(), ',');
                                                foreach (string value in values)
                                                {
                                                    ddldrop.Items.Add(value);
                                                }
                                                ddldrop.CssClass = "";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(ddldrop);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "RADIO":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                RadioButtonList rdr = new RadioButtonList();
                                                rdr.ID = "radio_" + row["fieldid"].ToString();
                                                rdr.DataSource = row["fieldvalue"].ToString().Split(',').ToList();
                                                rdr.SelectedValue = row["fieldvalue"].ToString().Split(',').ToList()[0];
                                                rdr.DataBind();
                                                rdr.CssClass = "";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(rdr);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "LABEL":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                Label LBL = new Label();
                                                LBL.ID = "LBL_" + row["fieldid"].ToString();
                                                LBL.CssClass = "";
                                                LBL.Text = ReplaceInputJsonValues(row["fieldvalue"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(new LiteralControl("<br />"));
                                                newForm.Controls.Add(LBL);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "DATE":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                TextBox txt = new TextBox();
                                                txt.ID = "txt_" + row["fieldid"].ToString();
                                                txt.CssClass = "form-control datecontrol";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(txt);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                if (dt.Rows[0]["formhtml"].ToString() != "")
                                {
                                    string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", "$('#disabledDiv :input').attr('disabled', true);", true);
                                }
                            }
                            if (dt.Rows[0]["formfooter"].ToString() != null && dt.Rows[0]["formfooter"].ToString() != "")
                            {
                                string footer = ReplaceInputJsonValues(dt.Rows[0]["formfooter"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + footer + "</div>"));
                            }
                            newForm.Controls.Add(new LiteralControl("<div class='form-group' style='font-weight:bold;'>"));
                            Label approval = new Label();
                            approval.Text = "APPROVED";
                            RadioButtonList approvalrdr = new RadioButtonList();
                            approvalrdr.ID = "rdrapproval";
                            approvalrdr.Items.Add(new ListItem("YES", "1"));
                            approvalrdr.Items.Add(new ListItem("NO", "2"));
                            approvalrdr.Items.Add(new ListItem("Manually Handled", "3"));
                            approvalrdr.SelectedValue = "1";
                            newForm.Controls.Add(approval);
                            newForm.Controls.Add(approvalrdr);
                            newForm.Controls.Add(new LiteralControl("</div>"));
                            if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                            {
                                newForm.Controls.Add(new LiteralControl("</div>"));
                            }
                            newForm.Controls.Add(new LiteralControl("<div class='form-group'  id='formsdiv' style='font-weight:bold;'>"));
                            Label lblforms = new Label();
                            lblforms.ID = "labelformselect";
                            lblforms.Text = "Map another Form ";
                            DropDownList dropdownform = new DropDownList();
                            dropdownform.ID = "drpdwnforms";
                            dt = obj.GetForms(Session["programid"].ToString());
                            ListItem li = new ListItem();
                            li.Value = "";
                            li.Text = "Select";
                            dropdownform.Items.Add(li);
                            if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                            {
                                foreach (DataRow form in dt.Rows)
                                {
                                    if (formid.ToString() != form["formid"].ToString())
                                    {
                                        li = new ListItem();
                                        li.Value = form["formid"].ToString();
                                        li.Text = form["formname"].ToString();
                                        dropdownform.Items.Add(li);
                                    }
                                }
                            }
                            dropdownform.CssClass = "form-control";
                            newForm.Controls.Add(lblforms);
                            newForm.Controls.Add(dropdownform);
                            newForm.Controls.Add(new LiteralControl("</div>"));
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<div style='text-align:center'>"));
                            Button btn = new Button();
                            btn.ID = "btnsubmit";
                            btn.Text = "SUBMIT";
                            btn.CssClass = "btn btn-success";
                            btn.OnClientClick = "javascript:saveStep();";
                            btn.CausesValidation = false;
                            btn.Click += new System.EventHandler(btn_Click);
                            newForm.Controls.Add(btn);
                            newForm.Controls.Add(new LiteralControl("</div>"));
                            log.Info("successfully created l2 approval form for formid-" + formid);
                        }
                        else
                        {
                            log.Info(Session["mail"].ToString() + " is not authorised L2 approver");
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this approval.";
                            Session["message_type"] = "Danger";
                            automationLi.Visible = false;
                            ApprovalLi.Visible = false;
                            MailLi.Visible = false;
                            existingFormLI.Visible = false;
                            ShowMessage();
                            return;
                        }
                        break;
                    case "SENTTOUSER":
                        log.Info("inside the SENTTOUSER switch case");
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        log.Info("formid for sysid--" + Session["sys"].ToString() + " is--" + formid);
                        try
                        {
                            log.Debug("trying to comapre session mailid with user in db");
                            if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                                isuser = true;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with user-" + ex.Message);
                            isuser = false;
                        }
                        if (isuser == true)
                        {
                            existingFormLI.Visible = true;
                            ApprovalLi.Visible = false;
                            MailLi.Visible = false;
                            objtable = obj.GetFormFields(formid.ToString());
                            if (objtable != null && objtable.Rows.Count > 0)
                            {
                                foreach (DataRow dr in objtable.Rows)
                                {
                                    hdnFields.Value = hdnFields.Value + dr["fieldname"].ToString() + ",";
                                }
                                if (hdnFields.Value.Length > 0)
                                {
                                    hdnFields.Value = hdnFields.Value.Substring(0, hdnFields.Value.Length - 1);
                                }
                            }
                            if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                            {
                                log.Info(Session["mail"].ToString() + " is a valid user");
                                try
                                {
                                    log.Debug("trying to get all form fields for formid-" + formid);
                                    log.Info("successfully got all info for form fields for formid--" + formid);
                                }
                                catch (Exception ex)
                                {
                                    log.Error("error while getting all form fields for formid--" + ex.Message);
                                    if (ex.InnerException != null)
                                    {
                                        log.Error(ex.InnerException.Message);
                                    }
                                }
                                request_type = dt.Rows[0]["ProgramType"].ToString();
                                tickettype = dt.Rows[0]["RequestType"].ToString();
                                newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                else
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                if (tickettype.Contains("TASK"))
                                {
                                    DataTable questiondatatable = new DataTable();
                                    questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                    if (questiondatatable.Rows.Count > 0)
                                    {
                                        newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                        foreach (DataRow dr in questiondatatable.Rows)
                                        {
                                            newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                        }
                                        newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                    }
                                }
                                newForm.Controls.Add(new LiteralControl("</table>"));
                                newForm.Controls.Add(new LiteralControl("</div>"));
                                if (dt.Rows[0]["formheader"].ToString() != null && dt.Rows[0]["formheader"].ToString() != "")
                                {
                                    string header = ReplaceInputJsonValues(dt.Rows[0]["formheader"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + header + "</div>"));
                                }
                                foreach (DataRow row in objtable.Rows)
                                {
                                    string value = string.Empty;
                                    Label lbl = new Label();
                                    lbl.Font.Bold = true;
                                    newForm.Controls.Add(new LiteralControl("<br/>"));
                                    string type = row["fieldtype"].ToString();
                                    switch (type)
                                    {
                                        case "TEXTBOX":
                                            try
                                            {
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                TextBox txt = new TextBox();
                                                txt.ID = "txt_" + row["fieldid"].ToString();
                                                if ((Boolean)row["isMandatory"] == true)
                                                    txt.Attributes.Add("required", "true");
                                                txt.CssClass = "form-control";
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                txt.Text = value;
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(txt);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "DROPDOWN":
                                            try
                                            {
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                DropDownList ddldrop = new DropDownList();
                                                ddldrop.ID = "ddl_" + row["fieldid"].ToString();
                                                string[] values = ToStringArray(row["fieldvalue"].ToString(), ',');
                                                foreach (string item in values)
                                                {
                                                    ddldrop.Items.Add(item);
                                                }
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                if (value != "" && ddldrop.Items.FindByText(value) != null)
                                                    ddldrop.Items.FindByText(value).Selected = true;
                                                ddldrop.CssClass = "form-control";
                                                ddldrop.Items.Insert(0, new ListItem("Select"));
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(ddldrop);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "CHECKBOXLIST":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                CheckBoxList ddldrop = new CheckBoxList();
                                                ddldrop.ID = "cbl_" + row["fieldid"].ToString();
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                List<string> lstSelected = new List<string>();
                                                if (!string.IsNullOrEmpty(value))
                                                {
                                                    lstSelected = value.Split(',').ToList();
                                                }
                                                string[] values = ToStringArray(row["fieldvalue"].ToString(), ',');
                                                foreach (string v in values)
                                                {
                                                    ListItem li = new ListItem();
                                                    li.Value = v;
                                                    li.Text = v;
                                                    if (lstSelected.Any(c => c == v))
                                                        li.Selected = true;
                                                    ddldrop.Items.Add(li);
                                                }
                                                ddldrop.CssClass = "";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(ddldrop);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "RADIO":
                                            try
                                            {
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                RadioButtonList rdr = new RadioButtonList();
                                                rdr.ID = "radio_" + row["fieldid"].ToString();
                                                rdr.DataSource = row["fieldvalue"].ToString().Split(',').ToList();
                                                rdr.DataBind();
                                                rdr.SelectedValue = row["fieldvalue"].ToString().Split(',').ToList()[0];
                                                rdr.CssClass = "";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(rdr);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "LABEL":
                                            try
                                            {
                                                log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                Label LBL = new Label();
                                                LBL.ID = "LBL_" + row["fieldid"].ToString();
                                                LBL.Text = ReplaceInputJsonValues(row["fieldvalue"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                                LBL.CssClass = "";
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(new LiteralControl("<br />"));
                                                newForm.Controls.Add(LBL);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                                log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        case "DATE":
                                            try
                                            {
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                TextBox txt = new TextBox();
                                                txt.ID = "txt_" + row["fieldid"].ToString();
                                                if ((Boolean)row["isMandatory"] == true)
                                                    txt.Attributes.Add("required", "true");
                                                txt.CssClass = "form-control datecontrol";
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                txt.Text = value;
                                                newForm.Controls.Add(lbl);
                                                newForm.Controls.Add(new LiteralControl("</>"));
                                                newForm.Controls.Add(txt);
                                                newForm.Controls.Add(new LiteralControl("</div>"));
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                log.Error(ex.Message);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                newForm.Controls.Add(new LiteralControl("</div>"));
                                if (dt.Rows[0]["formfooter"].ToString() != null && dt.Rows[0]["formfooter"].ToString() != "")
                                {
                                    string footer = ReplaceInputJsonValues(dt.Rows[0]["formfooter"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + footer + "</div>"));
                                }
                            }
                            else
                            {
                                if (dt.Rows[0]["formhtml"].ToString() != "")
                                {
                                    string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl(formhtml));
                                    int icount = 0;
                                    string scriptstring = string.Empty;
                                    if (objtable != null && objtable.Rows.Count > 0)
                                    {
                                        foreach (DataRow arr in objtable.Rows)
                                        {
                                            try
                                            {
                                                string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                if (!string.IsNullOrEmpty(value))
                                                {
                                                    if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                    {
                                                        scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                    }
                                                    else
                                                    {
                                                        scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                    }
                                                }
                                                icount++;
                                            }
                                            catch (Exception)
                                            {
                                                icount++;
                                            }
                                        }
                                        if (scriptstring != "")
                                        {
                                            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                        }
                                    }
                                }
                            }
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<div style='text-align:center'>"));
                            Button btn = new Button();
                            btn.ID = "btnsubmit";
                            btn.Text = "SUBMIT";
                            btn.CssClass = "btn btn-success";
                            btn.OnClientClick = "javascript:saveStep();";
                            btn.CausesValidation = false;
                            btn.Click += new System.EventHandler(btn_Click);
                            newForm.Controls.Add(btn);
                            newForm.Controls.Add(new LiteralControl("</div>"));
                        }
                        else
                        {
                            try
                            {
                                formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                                log.Debug("trying to comapre session mailid with l2 approver in db");
                                IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                                isl2 = isapprover;
                            }
                            catch (Exception ex)
                            {
                                log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                                isl2 = false;
                            }
                            if (isl2 || isAuthorized)
                            {
                                existingFormLI.Visible = true;
                                ApprovalLi.Visible = false;
                                MailLi.Visible = false;
                                formid = int.Parse(dt.Rows[0]["formid"].ToString());
                                requestid = dt.Rows[0]["requestid"].ToString();
                                request_type = dt.Rows[0]["ProgramType"].ToString();
                                tickettype = dt.Rows[0]["RequestType"].ToString();
                                if (dt.Rows[0]["formtype"].ToString().ToLower() == "expert")
                                {
                                    if (dt.Rows[0]["formhtml"].ToString() != "")
                                    {
                                        string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                        newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                        int icount = 0;
                                        string scriptstring = string.Empty;
                                        if (objtable != null && objtable.Rows.Count > 0)
                                        {
                                            foreach (DataRow arr in objtable.Rows)
                                            {
                                                try
                                                {
                                                    string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                    value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                    if (!string.IsNullOrEmpty(value))
                                                    {
                                                        if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                        {
                                                            scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                        }
                                                        else
                                                        {
                                                            scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                        }
                                                    }
                                                    icount++;
                                                }
                                                catch (Exception)
                                                {
                                                    icount++;
                                                }
                                            }
                                            if (scriptstring != "")
                                            {
                                                scriptstring = scriptstring + "$('#disabledDiv :input').attr('disabled', true);";
                                                ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                    newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                    newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                    if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                        newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                    else
                                        newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                    if (tickettype.Contains("TASK"))
                                    {
                                        DataTable questiondatatable = new DataTable();
                                        questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                        if (questiondatatable.Rows.Count > 0)
                                        {
                                            newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                            foreach (DataRow dr in questiondatatable.Rows)
                                            {
                                                newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                            }
                                            newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                        }
                                    }
                                    DataTable approvalhistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, "L2Approval");
                                    if (approvalhistory != null && approvalhistory.Rows != null && approvalhistory.Rows.Count > 0)
                                    {
                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>L2 Approver</b></td><td>" + approvalhistory.Rows[0]["Comment"].ToString().Split(':')[0] + "</td></tr>"));
                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>Approval Status</b></td><td>" + approvalhistory.Rows[0]["ApprovalStatus"] + "</td></tr>"));
                                    }
                                    newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                }
                            }
                            else
                            {
                                Session["message_title"] = "Unauthorized";
                                Session["message"] = "Not authorized for this approval.";
                                Session["message_type"] = "Danger";
                                ShowMessage();
                                automationLi.Visible = false;
                                ApprovalLi.Visible = false;
                                MailLi.Visible = false;
                                existingFormLI.Visible = false;
                                return;
                            }
                        }
                        break;
                    case "SENTFORAPPROVAL":
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORAPPROVAL":
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERCONFIRMATION":
                        try
                        {
                            log.Info("inside the SENTFORAPPROVAL switch case");
                            formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                            requestid = dt.Rows[0]["requestid"].ToString();
                            log.Info("formid for sysid--" + Session["sys"].ToString() + " is--" + formid);
                            try
                            {
                                log.Debug("trying to get all form fields for formid-" + formid);
                                objtable = obj.GetFormFields(formid.ToString());
                                if (objtable != null && objtable.Rows.Count > 0)
                                {
                                    foreach (DataRow dr in objtable.Rows)
                                    {
                                        hdnFields.Value = hdnFields.Value + dr["fieldname"].ToString() + ",";
                                    }
                                    if (hdnFields.Value.Length > 0)
                                    {
                                        hdnFields.Value = hdnFields.Value.Substring(0, hdnFields.Value.Length - 1);
                                    }
                                }
                                log.Info("successfully got all info for form fields for formid--" + formid);
                            }
                            catch (Exception ex)
                            {
                                log.Error("error while getting all form fields for formid--" + ex.Message);
                                if (ex.InnerException != null)
                                {
                                    log.Error(ex.InnerException.Message);
                                }
                            }
                            IsApproved = false;
                            log.Debug("trying to comapre session mailid with approver in db");
                            GroupName = string.Empty;
                            approversList = string.Empty;
                            try
                            {
                                
                                IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                                isl2 = isapprover;
                            }
                            catch (Exception ex)
                            {
                                log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                                isl2 = false;
                            }
                            IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), false, out GroupName, out approversList);
                            if (isapprover == true || isExceptionalApprover == true || isl2)
                            {
                                existingFormLI.Visible = true;
                                ApprovalLi.Visible = true;
                                MailLi.Visible = true;
                                if (status == "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORAPPROVAL")
                                {
                                    automationLi.Visible = true;
                                }
                                if (status == "SENTFORAPPROVAL")
                                {
                                    automationLi.Visible = false;
                                }
                                if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                                {
                                    newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                    newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                    newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                    if (!IsApproved)
                                    {
                                        request_type = dt.Rows[0]["ProgramType"].ToString();
                                        tickettype = dt.Rows[0]["RequestType"].ToString();
                                        if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                        else
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                        if (tickettype.Contains("TASK"))
                                        {
                                            DataTable questiondatatable = new DataTable();
                                            questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                            if (questiondatatable.Rows.Count > 0)
                                            {
                                                newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                                foreach (DataRow dr in questiondatatable.Rows)
                                                {
                                                    newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                                }
                                                newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                            }
                                        }
                                        newForm.Controls.Add(new LiteralControl("</table>"));
                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                        if (dt.Rows[0]["formheader"].ToString() != null && dt.Rows[0]["formheader"].ToString() != "")
                                        {
                                            string header = ReplaceInputJsonValues(dt.Rows[0]["formheader"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                            newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + header + "</div>"));
                                        }
                                        foreach (DataRow row in objtable.Rows)
                                        {
                                            string value = string.Empty;
                                            Label lbl = new Label();
                                            lbl.Font.Bold = true;
                                            newForm.Controls.Add(new LiteralControl("<br/>"));
                                            string type = row["fieldtype"].ToString();
                                            switch (type)
                                            {
                                                case "TEXTBOX":
                                                    try
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        TextBox txt = new TextBox();
                                                        txt.ID = "txt_" + row["fieldid"].ToString();
                                                        
                                                        txt.CssClass = "form-control";
                                                        value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                        txt.Text = value;
                                                        txt.ReadOnly = true;
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(new LiteralControl("</>"));
                                                        newForm.Controls.Add(txt);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                case "DROPDOWN":
                                                    try
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        TextBox txt = new TextBox();
                                                        txt.ID = "txt_" + row["fieldid"].ToString();
                                                        
                                                        txt.CssClass = "form-control";
                                                        value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                        txt.Text = value;
                                                        txt.ReadOnly = true;
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(new LiteralControl("</>"));
                                                        newForm.Controls.Add(txt);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                case "CHECKBOXLIST":
                                                    try
                                                    {
                                                        log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        TextBox txtbx = new TextBox();
                                                        txtbx.ID = "txt_" + row["fieldname"].ToString();
                                                        if (row["fieldname"].ToString() == "GROUP")//if multiple approval
                                                        {
                                                            txtbx.Text = GroupName;
                                                        }
                                                        else
                                                        {
                                                            txtbx.Text = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                        }
                                                        txtbx.CssClass = "form-control";
                                                        txtbx.Enabled = false;
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(txtbx);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                        log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                case "RADIO":
                                                    try
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        RadioButtonList rdr = new RadioButtonList();
                                                        rdr.ID = "radio_" + row["fieldid"].ToString();
                                                        rdr.DataSource = row["fieldvalue"].ToString().Split(',').ToList();
                                                        rdr.DataBind();
                                                        rdr.Enabled = false;
                                                        rdr.SelectedValue = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                        rdr.CssClass = "";
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(rdr);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                case "LABEL":
                                                    try
                                                    {
                                                        log.Debug("trying to create  field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        Label LBL = new Label();
                                                        LBL.ID = "LBL_" + row["fieldid"].ToString();
                                                        LBL.Text = ReplaceInputJsonValues(row["fieldvalue"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                                        LBL.CssClass = "";
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(new LiteralControl("</>"));
                                                        newForm.Controls.Add(new LiteralControl("<br />"));
                                                        newForm.Controls.Add(LBL);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                        log.Info("successfully created field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                case "DATE":
                                                    try
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                                        lbl.Text = !string.IsNullOrEmpty(row["FieldQuestion"].ToString()) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString();
                                                        TextBox txt = new TextBox();
                                                        txt.ID = "txt_" + row["fieldid"].ToString();
                                                       
                                                        txt.CssClass = "form-control";
                                                        value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                        txt.Text = value;
                                                        txt.ReadOnly = true;
                                                        newForm.Controls.Add(lbl);
                                                        newForm.Controls.Add(new LiteralControl("</>"));
                                                        newForm.Controls.Add(txt);
                                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        log.Error("error while creating field with id-" + row["fieldid"].ToString() + " for formid-" + formid);
                                                        log.Error(ex.Message);
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                        newForm.Controls.Add(new LiteralControl("<div class='form-group' style='font-weight:bold;' >"));
                                        Label approverApproval = new Label();
                                        if (status == "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERCONFIRMATION")
                                        {
                                            string UserConfirmationText = string.Empty;
                                            try
                                            {
                                                UserConfirmationText = ConfigurationManager.AppSettings["UserConfirmationText"].ToString();
                                            }
                                            catch (Exception)
                                            {
                                                UserConfirmationText = "We need your confirmation before we can close this request. Please confirm if this request can be closed?";
                                            }
                                            approverApproval.Text = UserConfirmationText;
                                        }
                                        else
                                            approverApproval.Text = "APPROVED";
                                        RadioButtonList approverrdr = new RadioButtonList();
                                        approverrdr.ID = "rdrapproval";
                                        approverrdr.Items.Add(new ListItem("YES", "1"));
                                        approverrdr.Items.Add(new ListItem("NO", "2"));
                                        approverrdr.SelectedValue = "1";
                                        newForm.Controls.Add(approverApproval);
                                        newForm.Controls.Add(approverrdr);
                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'  id='reason' style='font-weight:bold;'>"));
                                        Label reason = new Label();
                                        reason.ID = "labelreason";
                                        reason.Text = "Reason ";
                                        TextBox txtreason = new TextBox();
                                        txtreason.ID = "txtreason";
                                        txtreason.CssClass = "form-control";
                                        newForm.Controls.Add(reason);
                                        newForm.Controls.Add(txtreason);
                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                        if (dt.Rows[0]["formfooter"].ToString() != null && dt.Rows[0]["formfooter"].ToString() != "")
                                        {
                                            string footer = ReplaceInputJsonValues(dt.Rows[0]["formfooter"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                            newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + footer + "</div>"));
                                        }
                                        newForm.Controls.Add(new LiteralControl("<br/>"));
                                        newForm.Controls.Add(new LiteralControl("<br/>"));
                                        newForm.Controls.Add(new LiteralControl("<div style='text-align:center'>"));
                                        Button btn = new Button();
                                        btn.ID = "btnsubmit";
                                        btn.Text = "SUBMIT";
                                        btn.CssClass = "btn btn-success";
                                        btn.OnClientClick = "javascript:saveStep();";
                                        btn.CausesValidation = false;
                                        btn.Click += new System.EventHandler(btn_Click);
                                        newForm.Controls.Add(btn);
                                        newForm.Controls.Add(new LiteralControl("</div>"));
                                    }
                                    else
                                    {
                                        request_type = dt.Rows[0]["ProgramType"].ToString();
                                        tickettype = dt.Rows[0]["RequestType"].ToString();
                                        string value = string.Empty;
                                        if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                        else
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                        if (tickettype.Contains("TASK"))
                                        {
                                            DataTable questiondatatable = new DataTable();
                                            questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                            if (questiondatatable.Rows.Count > 0)
                                            {
                                                newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                                foreach (DataRow dr in questiondatatable.Rows)
                                                {
                                                    newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                                }
                                                newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                            }
                                        }
                                        foreach (DataRow row in objtable.Rows)
                                        {
                                            if (row["fieldname"].ToString() != "GROUP")
                                            {
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                newForm.Controls.Add(new LiteralControl("<tr> <td><b>" + ((!string.IsNullOrEmpty(row["FieldQuestion"].ToString())) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString()) + "</b></td><td>" + value + "</td></tr>"));
                                            }
                                        }
                                        DataTable approvalhistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, "GroupApproval");
                                        string ColumnName = "Group";
                                        if (approvalhistory != null && approvalhistory.Rows != null && approvalhistory.Rows.Count == 0)
                                        {
                                            ColumnName = "Approval Level";
                                            approvalhistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, "MultiLevelApproval");
                                        }
                                        if (approvalhistory != null && approvalhistory.Rows != null && approvalhistory.Rows.Count > 0)
                                        {
                                            foreach (DataRow dr in approvalhistory.Rows)
                                            {
                                                if (dr["Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                                {
                                                    if (!string.IsNullOrEmpty(dr["Comment"].ToString()) && dr["Comment"].ToString().Contains(":"))
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>" + ColumnName + "</b></td><td>" + dr["GroupName"].ToString() + "</td></tr>"));
                                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>Approver</b></td><td>" + dr["Comment"].ToString().Split(':')[0] + "</td></tr>"));
                                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>Approval Status</b></td><td>" + dr["ApprovalStatus"] + "</td></tr>"));
                                                        if (!string.IsNullOrEmpty(dr["Comment"].ToString().Split(':')[1]))
                                                            newForm.Controls.Add(new LiteralControl("<tr> <td><b>Reason</b></td><td>" + dr["Comment"].ToString().Split(':')[1] + "</td></tr>"));
                                                    }
                                                    else
                                                    {
                                                        newForm.Controls.Add(new LiteralControl("<tr> <td><b>Status</b></td><td>" + dt.Rows[0]["status"].ToString() + "</td></tr>"));
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                    }
                                }
                                else
                                {
                                    if (!IsApproved)
                                    {
                                        if (dt.Rows[0]["formhtml"].ToString() != "")
                                        {
                                            string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                            newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                            int icount = 0;
                                            string scriptstring = string.Empty;
                                            if (objtable != null && objtable.Rows.Count > 0)
                                            {
                                                foreach (DataRow arr in objtable.Rows)
                                                {
                                                    try
                                                    {
                                                        string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                        value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                        if (!string.IsNullOrEmpty(value))
                                                        {
                                                            if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                            }
                                                            else
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                                
                                                            }
                                                        }
                                                        icount++;
                                                    }
                                                    catch (Exception)
                                                    {
                                                        icount++;
                                                    }
                                                }
                                                if (scriptstring != "")
                                                {
                                                    scriptstring = scriptstring + "$('#disabledDiv :input').attr('disabled', true);";
                                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                                }
                                            }
                                            newForm.Controls.Add(new LiteralControl("<div class='form-group' style='font-weight:bold;'>"));
                                            Label approverApproval = new Label();
                                            if (status == "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERCONFIRMATION")
                                            {
                                                string UserConfirmationText = string.Empty;
                                                try
                                                {
                                                    UserConfirmationText = ConfigurationManager.AppSettings["UserConfirmationText"].ToString();
                                                }
                                                catch (Exception)
                                                {
                                                    UserConfirmationText = "We need your confirmation before we can close this request. Please confirm if this request can be closed?";
                                                }
                                                approverApproval.Text = UserConfirmationText;
                                            }
                                            else
                                                approverApproval.Text = "APPROVED";
                                            RadioButtonList approverrdr = new RadioButtonList();
                                            approverrdr.ID = "rdrapproval";
                                            approverrdr.Items.Add(new ListItem("YES", "1"));
                                            approverrdr.Items.Add(new ListItem("NO", "2"));
                                            approverrdr.SelectedValue = "1";
                                            newForm.Controls.Add(approverApproval);
                                            newForm.Controls.Add(approverrdr);
                                            newForm.Controls.Add(new LiteralControl("</div>"));
                                            newForm.Controls.Add(new LiteralControl("<div class='form-group'  id='reason' style='font-weight:bold;'>"));
                                            Label reason = new Label();
                                            reason.ID = "labelreason";
                                            reason.Text = "Reason ";
                                            TextBox txtreason = new TextBox();
                                            txtreason.ID = "txtreason";
                                            txtreason.CssClass = "form-control";
                                            newForm.Controls.Add(reason);
                                            newForm.Controls.Add(txtreason);
                                            newForm.Controls.Add(new LiteralControl("</div>"));
                                            if (dt.Rows[0]["formfooter"].ToString() != null && dt.Rows[0]["formfooter"].ToString() != "")
                                            {
                                                string footer = ReplaceInputJsonValues(dt.Rows[0]["formfooter"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>" + footer + "</div>"));
                                            }
                                            newForm.Controls.Add(new LiteralControl("<br/>"));
                                            newForm.Controls.Add(new LiteralControl("<br/>"));
                                            newForm.Controls.Add(new LiteralControl("<div style='text-align:center'>"));
                                            Button btn = new Button();
                                            btn.ID = "btnsubmit";
                                            btn.Text = "SUBMIT";
                                            btn.CssClass = "btn btn-success";
                                            btn.OnClientClick = "javascript:saveStep();";
                                            btn.CausesValidation = false;
                                            btn.Click += new System.EventHandler(btn_Click);
                                            newForm.Controls.Add(btn);
                                            newForm.Controls.Add(new LiteralControl("</div>"));
                                        }
                                    }
                                    else
                                    {
                                        request_type = dt.Rows[0]["ProgramType"].ToString();
                                        tickettype = dt.Rows[0]["RequestType"].ToString();
                                        if (dt.Rows[0]["formhtml"].ToString() != "")
                                        {
                                            string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                            newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                            int icount = 0;
                                            string scriptstring = string.Empty;
                                            if (objtable != null && objtable.Rows.Count > 0)
                                            {
                                                foreach (DataRow arr in objtable.Rows)
                                                {
                                                    try
                                                    {
                                                        string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                        value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                        if (!string.IsNullOrEmpty(value))
                                                        {
                                                            if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                            }
                                                            else
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                            }
                                                        }
                                                        icount++;
                                                    }
                                                    catch (Exception)
                                                    {
                                                        icount++;
                                                    }
                                                }
                                                if (scriptstring != "")
                                                {
                                                    scriptstring = scriptstring + "$('#disabledDiv :input').attr('disabled', true);";
                                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                                    log.Debug("trying to comapre session mailid with l2 approver in db");
                                    IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                                    isl2 = isapprover;
                                }
                                catch (Exception ex)
                                {
                                    log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                                    isl2 = false;
                                }
                                try
                                {
                                    log.Debug("trying to comapre session mailid with user in db");
                                    if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                                        isuser = true;
                                }
                                catch (Exception ex)
                                {
                                    log.Error("error while comparing session mailid with user-" + ex.Message);
                                    isuser = false;
                                }
                                if (isuser || isl2 || isAuthorized)
                                {
                                    automationLi.Visible = true;
                                    existingFormLI.Visible = true;
                                    ApprovalLi.Visible = true;
                                    MailLi.Visible = true;
                                    formid = int.Parse(dt.Rows[0]["formid"].ToString());
                                    requestid = dt.Rows[0]["requestid"].ToString();
                                    request_type = dt.Rows[0]["ProgramType"].ToString();
                                    tickettype = dt.Rows[0]["RequestType"].ToString();
                                    if (dt.Rows[0]["formtype"].ToString().ToLower() == "expert")
                                    {
                                        if (dt.Rows[0]["formhtml"].ToString() != "")
                                        {
                                            string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                            newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                            int icount = 0;
                                            string scriptstring = string.Empty;
                                            if (objtable != null && objtable.Rows.Count > 0)
                                            {
                                                foreach (DataRow arr in objtable.Rows)
                                                {
                                                    try
                                                    {
                                                        string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                        value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                        if (!string.IsNullOrEmpty(value))
                                                        {
                                                            if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                            }
                                                            else
                                                            {
                                                                scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                            }
                                                        }
                                                        icount++;
                                                    }
                                                    catch (Exception)
                                                    {
                                                        icount++;
                                                    }
                                                }
                                                if (scriptstring != "")
                                                {
                                                    scriptstring = scriptstring + "$('#disabledDiv :input').attr('disabled', true);";
                                                    ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                        newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                        newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                        if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                        else
                                            newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                        if (tickettype.Contains("TASK"))
                                        {
                                            DataTable questiondatatable = new DataTable();
                                            questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                            if (questiondatatable.Rows.Count > 0)
                                            {
                                                newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                                foreach (DataRow dr in questiondatatable.Rows)
                                                {
                                                    newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                                }
                                                newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                            }
                                        }
                                        if (isuser || isAuthorized)
                                        {
                                            existingFormLI.Visible = true;
                                            ApprovalLi.Visible = true;
                                            MailLi.Visible = true;
                                            automationLi.Visible = true;
                                            objtable = obj.GetFormFields(formid.ToString());
                                            string value = string.Empty;
                                            foreach (DataRow row in objtable.Rows)
                                            {
                                                value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                                newForm.Controls.Add(new LiteralControl("<tr> <td><b>" + ((!string.IsNullOrEmpty(row["FieldQuestion"].ToString())) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString()) + "</b></td><td>" + value + "</td></tr>"));
                                            }
                                            newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                        }
                                        else if (isl2 || isAuthorized)
                                        {
                                            existingFormLI.Visible = true;
                                            ApprovalLi.Visible = true;
                                            MailLi.Visible = true;
                                            automationLi.Visible = true;
                                            DataTable approvalhistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, "L2Approval");
                                            if (approvalhistory != null && approvalhistory.Rows != null && approvalhistory.Rows.Count > 0)
                                            {
                                                newForm.Controls.Add(new LiteralControl("<tr> <td><b>L2 Approver</b></td><td>" + approvalhistory.Rows[0]["Comment"].ToString().Split(':')[0] + "</td></tr>"));
                                                newForm.Controls.Add(new LiteralControl("<tr> <td><b>Approval Status</b></td><td>" + approvalhistory.Rows[0]["ApprovalStatus"] + "</td></tr>"));
                                            }
                                            newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                        }
                                    }
                                }
                                else
                                {
                                    Session["message_title"] = "Unauthorized";
                                    Session["message"] = "Not authorized for this approval.";
                                    Session["message_type"] = "Danger";
                                    ShowMessage();
                                    automationLi.Visible = false;
                                    ApprovalLi.Visible = false;
                                    MailLi.Visible = false;
                                    existingFormLI.Visible = false;
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                            log.Error(ex.StackTrace);
                            automationLi.Visible = false;
                            ApprovalLi.Visible = false;
                            MailLi.Visible = false;
                            existingFormLI.Visible = false;
                        }
                        break;
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERINPUT":
                        log.Info("inside the SENTFORUSERINPUT switch case");
                        bool isUserAuthorized = false;
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        log.Info("formid for sysid--" + Session["sys"].ToString() + " is--" + formid);
                        if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                            isuser = true;
                        DataTable dtstep = obj.GetLastStepDetails(requestid, Session["programid"].ToString());
                        string htmlUser = dtstep.Rows[0]["InputJsonUser"].ToString();
                        try
                        {
                            log.Debug("trying to comapre session mailid with user in db");
                            if (dtstep.Rows[0]["Stepinput"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                isUserAuthorized = true;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with user-" + ex.Message);
                            isUserAuthorized = false;
                        }
                        if (isUserAuthorized == true)
                        {
                            existingFormLI.Visible = true;
                            ApprovalLi.Visible = true;
                            MailLi.Visible = true;
                            automationLi.Visible = true;
                            if (!string.IsNullOrEmpty(htmlUser))
                            {
                                try
                                {
                                    hdnFields.Value = "";
                                    HtmlDocument doc = new HtmlDocument();
                                    doc.LoadHtml(htmlUser);
                                    HtmlNodeCollection col = doc.DocumentNode.SelectNodes(".//input");
                                    if (col != null)
                                    {
                                        foreach (HtmlNode node in col)
                                        {
                                            try
                                            {
                                                string id = node.Attributes["id"].Value;
                                                hdnFields.Value = hdnFields.Value + id + ",";
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error(ex.Message);
                                            }
                                        }
                                    }
                                    col = doc.DocumentNode.SelectNodes(".//select");
                                    if (col != null)
                                    {
                                        foreach (HtmlNode node in col)
                                        {
                                            try
                                            {
                                                string id = node.Attributes["id"].Value;
                                                hdnFields.Value = hdnFields.Value + id + ",";
                                            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                                            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                                            {
                                            }
                                        }
                                    }
                                    col = doc.DocumentNode.SelectNodes(".//textarea");
                                    if (col != null)
                                    {
                                        foreach (HtmlNode node in col)
                                        {
                                            try
                                            {
                                                string id = node.Attributes["id"].Value;
                                                hdnFields.Value = hdnFields.Value + id + ",";
                                            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
                                            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
                                            {
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                                if (hdnFields.Value.Length > 0)
                                {
                                    hdnFields.Value = hdnFields.Value.Substring(0, hdnFields.Value.Length - 1);
                                }
                            }
                            newForm.Controls.Add(new LiteralControl(ReplaceInputJsonValues(htmlUser, dt.Rows[0]["formname"].ToString(), requestid)));
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<br/>"));
                            newForm.Controls.Add(new LiteralControl("<div style='text-align:center'>"));
                            Button btn = new Button();
                            btn.ID = "btnsubmit";
                            btn.Text = "SUBMIT";
                            btn.CssClass = "btn btn-success";
                            btn.OnClientClick = "javascript:saveStepUserInput();";
                            btn.CausesValidation = false;
                            btn.Click += new System.EventHandler(btn_Click);
                            newForm.Controls.Add(btn);
                            newForm.Controls.Add(new LiteralControl("</div>"));
                        }
                        else
                        {
                            try
                            {
                                formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                                log.Debug("trying to comapre session mailid with l2 approver in db");
                                IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                                isl2 = isapprover;
                            }
                            catch (Exception ex)
                            {
                                log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                                isl2 = false;
                            }
                            DataTable dtapprovalHistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, false);
                            if (dtapprovalHistory != null && dtapprovalHistory.Rows.Count > 0)
                            {
                                foreach (DataRow dr in dtapprovalHistory.Rows)
                                {
                                    if (dr["Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                    {
                                        isapprover = true;
                                        break;
                                    }
                                }
                            }
                            if (isl2 || isAuthorized || isapprover || isuser)
                            {
                                existingFormLI.Visible = true;
                                ApprovalLi.Visible = true;
                                MailLi.Visible = true;
                                automationLi.Visible = true;
                                formid = int.Parse(dt.Rows[0]["formid"].ToString());
                                requestid = dt.Rows[0]["requestid"].ToString();
                                request_type = dt.Rows[0]["ProgramType"].ToString();
                                tickettype = dt.Rows[0]["RequestType"].ToString();
                                newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + ReplaceInputJsonValues(htmlUser, dt.Rows[0]["formname"].ToString(), requestid) + "</div>"));
                            }
                            else
                            {
                                Session["message_title"] = "Unauthorized";
                                Session["message"] = "Not authorized for this request.";
                                Session["message_type"] = "Danger";
                                ShowMessage();
                                automationLi.Visible = false;
                                ApprovalLi.Visible = false;
                                MailLi.Visible = false;
                                existingFormLI.Visible = false;
                                return;
                            }
                        }
                        break;
                    default:
                        try
                        {
                            formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                            log.Debug("trying to comapre session mailid with l2 approver in db");
                            IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                            isl2 = isapprover;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with l2 approver-" + ex.Message);
                            isl2 = false;
                        }
                        try
                        {
                            log.Debug("trying to comapre session mailid with user in db");
                            if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                                isuser = true;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with user-" + ex.Message);
                            isuser = false;
                        }
                        objtable = obj.GetFormFields(formid.ToString());
                        if (dt.Rows[0]["formapprover"].ToString() == "multiple" && objtable != null && objtable.Rows != null && objtable.Rows.Count > 0)
                        {
                            foreach (DataRow dr in objtable.Rows)
                            {
                                if (dr["fieldname"].ToString() == "GROUP")
                                {
                                    if (dr["fieldvalueapprover"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                    {
                                        isapprover = true;
                                        List<string> groups = dr["fieldvalue"].ToString().Split(',').ToList();
                                        List<string> approvallist = dr["fieldvalueapprover"].ToString().Split(',').ToList();
                                        int index = approvallist.IndexOf(Session["mail"].ToString());
                                        string gp = "";
                                        if (index != -1)
                                            gp = groups[index];
                                        else
                                        {
                                            foreach (var approver in approvallist)
                                            {
                                                if (approver.ToLower().Contains(Session["mail"].ToString().ToLower()))
                                                {
                                                    index = approvallist.IndexOf(approver);
                                                    gp = groups[index];
                                                    break;
                                                }
                                            }
                                        }
                                        DataTable fieldvalues = obj.getfielddata("GROUP", requestid);
                                        if (fieldvalues != null && fieldvalues.Rows != null && fieldvalues.Rows.Count > 0)
                                        {
                                            List<string> selectedgroups = fieldvalues.Rows[0]["fieldvalue"].ToString().Split(',').ToList();
                                            List<string> approvals = fieldvalues.Rows[0]["fieldvalueapproval"].ToString().Split(',').ToList();
                                            index = selectedgroups.IndexOf(gp);
                                            if (index != -1 && approvals[index] != "NA")
                                            {
                                            }
                                            else if (index == -1)
                                            {
                                                isapprover = false;
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                        else if (dt.Rows[0]["formapprovaltype"].ToString() == "1")
                        {
                            if (Session["mail"].ToString().ToLower().Equals(GetManagerEmail(dt.Rows[0]["createdby"].ToString()).ToLower()))
                                isapprover = true;
                        }
                        else if (dt.Rows[0]["formapprovaltype"].ToString() == "5")//if multilevel approval
                        {
                            bool IsApprovedN = false;
                            IsMultiLevelApprover(dt, out isapprover, out IsApprovedN, Session["mail"].ToString());
                        }
                        else if (dt.Rows[0]["formapprover"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                            isapprover = true;
                        if (!isapprover)
                        {
                            DataTable dtapprovalHistory = obj.GetApprovalHistory(Session["programid"].ToString(), requestid, false);
                            if (dtapprovalHistory != null && dtapprovalHistory.Rows.Count > 0)
                            {
                                foreach (DataRow dr in dtapprovalHistory.Rows)
                                {
                                    if (dr["Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                    {
                                        isapprover = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isapprover || isuser || isl2 || isAuthorized)
                        {
                            existingFormLI.Visible = true;
                            ApprovalLi.Visible = true;
                            MailLi.Visible = true;
                            automationLi.Visible = true;
                            request_type = dt.Rows[0]["ProgramType"].ToString();
                            tickettype = dt.Rows[0]["RequestType"].ToString();
                            formid = int.Parse(dt.Rows[0]["formid"].ToString());
                            requestid = dt.Rows[0]["requestid"].ToString();
                            if (dt.Rows[0]["formtype"].ToString().ToLower() == "expert")
                            {
                                if (dt.Rows[0]["formhtml"].ToString() != "")
                                {
                                    string formhtml = ReplaceInputJsonValues(dt.Rows[0]["formhtml"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString());
                                    newForm.Controls.Add(new LiteralControl("<div id='disabledDiv'>" + formhtml + "</div>"));
                                    int icount = 0;
                                    string scriptstring = string.Empty;
                                    if (objtable != null && objtable.Rows.Count > 0)
                                    {
                                        foreach (DataRow arr in objtable.Rows)
                                        {
                                            try
                                            {
                                                string value = obj.getfieldvalueExpert(arr["fieldname"].ToString(), requestid);
                                                value = value.Replace("'", "\\'").Replace("\n", "\\n");
                                                if (!string.IsNullOrEmpty(value))
                                                {
                                                    if (arr["FieldType"].ToString().ToLower().Contains("checkbox") || arr["FieldType"].ToString().ToLower().Contains("radio"))
                                                    {
                                                        scriptstring = scriptstring + string.Format("try{{$('#{0}').prop('checked', true);}}catch(e){{}}", arr["fieldname"].ToString());
                                                    }
                                                    else
                                                    {
                                                        scriptstring = scriptstring + string.Format("try{{document.getElementById('{0}').value = '{1}';}}catch(e){{}}", arr["fieldname"].ToString(), value);
                                                    }
                                                }
                                                icount++;
                                            }
                                            catch (Exception)
                                            {
                                                icount++;
                                            }
                                        }
                                        if (scriptstring != "")
                                        {
                                            scriptstring = scriptstring + "$('#disabledDiv :input').attr('disabled', true);";
                                            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", scriptstring, true);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                newForm.Controls.Add(new LiteralControl("<div class='box-header with-border' style='text-align:center;font-weight:bolder'><b><h1 class='box-title'>" + dt.Rows[0]["formname"].ToString() + "</h1></b></div>"));
                                newForm.Controls.Add(new LiteralControl("<div class='box-body'>"));
                                newForm.Controls.Add(new LiteralControl("<div class='form-group'>"));
                                if (request_type == "PUSH" && objtable != null && objtable.Rows.Count > 0)
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr>"));
                                else
                                    newForm.Controls.Add(new LiteralControl(" <table class='table table-striped'> <tr> <td><b>REQUESTID</b></td><td>" + requestid + "</td></tr><tr> <td><b>CREATED BY</b></td><td>" + dt.Rows[0]["createdby"].ToString() + "</td></tr><tr> <td><b>SUMMARY</b></td><td>" + dt.Rows[0]["summary"].ToString() + "</td></tr><tr> <td><b>DESCRIPTION</b></td><td>" + dt.Rows[0]["description"].ToString().Replace(Environment.NewLine, "<br/>") + "</td></tr>"));
                                if (tickettype.Contains("TASK"))
                                {
                                    DataTable questiondatatable = new DataTable();
                                    questiondatatable = obj.getQuestions(dt.Rows[0]["requestitem"].ToString());
                                    if (questiondatatable.Rows.Count > 0)
                                    {
                                        newForm.Controls.Add(new LiteralControl("<tr><td><b>ADDITIONAL INFORMATION</b></td><td>"));
                                        foreach (DataRow dr in questiondatatable.Rows)
                                        {
                                            newForm.Controls.Add(new LiteralControl(dr["questiontext"].ToString() + " - " + dr["answer"].ToString() + "<br/>"));
                                        }
                                        newForm.Controls.Add(new LiteralControl("</td></tr>"));
                                    }
                                }
                                if (isapprover || isuser || isAuthorized)
                                {
                                    objtable = obj.GetFormFields(formid.ToString());
                                    string value = string.Empty;
                                    foreach (DataRow row in objtable.Rows)
                                    {
                                        if (row["fieldname"].ToString() != "GROUP" || isuser)
                                        {
                                            value = obj.getfieldvalue(Convert.ToInt32(row["fieldid"].ToString()), requestid);
                                            newForm.Controls.Add(new LiteralControl("<tr> <td><b>" + ((!string.IsNullOrEmpty(row["FieldQuestion"].ToString())) ? ReplaceInputJsonValues(row["FieldQuestion"].ToString(), dt.Rows[0]["formname"].ToString(), Session["requestid"].ToString()) : row["fieldname"].ToString()) + "</b></td><td>" + value + "</td></tr>"));
                                        }
                                    }
                                    newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                }
                                else if (isl2)
                                {
                                    automationLi.Visible = true;
                                    newForm.Controls.Add(new LiteralControl("</table></div></div>"));
                                }
                            }
                        }
                        else
                        {
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this approval.";
                            Session["message_type"] = "Danger";
                            ShowMessage();
                            automationLi.Visible = false;
                            ApprovalLi.Visible = false;
                            MailLi.Visible = false;
                            existingFormLI.Visible = false;
                        }
                        return;
                }
            }
            catch (Exception)
            {
                SessionHandler.LogOut();
            }
        }
        /// <summary>
        /// converting a comma separated string to array of string
        /// </summary>
        /// <param name="value"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        static string[] ToStringArray(string value, char separator)
        {
            return Array.ConvertAll(value.Split(separator), s => (s));
        }
        /// <summary>
        /// method to show meaasge
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
                log.Error("error while getting message details.-" + ee.Message);
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
        /// returns manager email
        /// </summary>
        /// <param name="useremail"></param>
        /// <returns></returns>
        public string GetManagerEmail(string useremail)
        {
            string manageremail = string.Empty;
            DataTable GP = obj.GetGlobalParameters(Session["programid"].ToString());
            if (GP != null && GP.Rows != null && GP.Rows.Count > 0)
            {
                DirectoryEntry _objDE = new DirectoryEntry("LDAP://" + GP.Rows[0]["adroot"].ToString());
                _objDE.Username = GP.Rows[0]["aduser"].ToString();
                _objDE.Password = ApprovalWorkflowApp.Utils.Crypto.Decrypt(GP.Rows[0]["adpassword"].ToString());
                _objDE.AuthenticationType = AuthenticationTypes.Secure;
                DirectorySearcher _objSer = new DirectorySearcher(_objDE);
                _objSer.Filter = "(&(objectClass=user)(mail=" + useremail + "*))";
                SearchResultCollection resultCollection = _objSer.FindAll();
                foreach (SearchResult _results in resultCollection)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(_results.Path))
                        {
                            string managerstring = GetProperty(_results, "manager");
                            if (!string.IsNullOrEmpty(managerstring) && managerstring.Contains(','))
                            {
                                managerstring = managerstring.Split(',')[0];
                                if (managerstring.Contains('='))
                                {
                                    managerstring = managerstring.Split('=')[1];
                                    if (!string.IsNullOrEmpty(managerstring))
                                    {
                                        _objSer.Filter = "(&(objectClass=user)(samaccountname=" + managerstring + "*))";
                                        SearchResultCollection resultCollectionNew = _objSer.FindAll();
                                        foreach (SearchResult _resultsnew in resultCollectionNew)
                                        {
                                            if (!string.IsNullOrEmpty(_resultsnew.Path))
                                            {
                                                manageremail = GetProperty(_resultsnew, "mail");
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        manageremail = useremail;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        log.Error("Error while getting manager email--" + ee.Message);
                    }
                }
            }
            return manageremail;
        }
        /// <summary>
        /// gets property from ldap
        /// </summary>
        /// <param name="searchResult"></param>
        /// <param name="PropertyName"></param>
        /// <returns></returns>
        public String GetProperty(SearchResult searchResult, string PropertyName)
        {
            StringBuilder x = new StringBuilder();
            if (searchResult.Properties.Contains(PropertyName))
            {
                for (int i = 0; i < searchResult.Properties[PropertyName].Count; i++)
                {
                    if (x.Length.Equals(0))
                    {
                        x.Append(searchResult.Properties[PropertyName][i].ToString());
                    }
                    else
                    {
                        x.Append(Environment.NewLine);
                        x.Append(searchResult.Properties[PropertyName][i].ToString());
                    }
                }
            }
            return x.ToString();
        }
        /// <summary>
        /// generates approval status table
        /// </summary>
        /// <param name="approvalhistory"></param>
        /// <returns></returns>
        private string GenerateApprovalStatusTableHtml(DataTable approvalhistory)
        {
            StringBuilder html = new StringBuilder();
            if (approvalhistory != null && approvalhistory.Rows != null && approvalhistory.Rows.Count > 0)
            {
                html.Append("<br/><div>");
                string Comments = string.Empty;
                string approver = string.Empty;
                html.Append("<table style=\"font-family: Trebuchet MS;border-collapse: collapse; font-size: 10pt\" cellspacing=0 cellpadding=5>");
                //add header row
                html.Append("<tr style='background-color: midnightblue; color:white'><td <td style='border: 1px solid black;padding:5px'>Group</td><td <td style='border: 1px solid black;padding:5px'>Approver</td><td <td style='border: 1px solid black;padding:5px'>Approval Status</td><td <td style='border: 1px solid black;padding:5px'>Comments</td></tr>");
                foreach (DataRow item in approvalhistory.Rows)
                {
                    if (item["Comment"].ToString().Split(':').ToList().Count > 1)
                    {
                        approver = item["Comment"].ToString().Split(':').ToList()[0];
                        Comments = item["Comment"].ToString().Split(':').ToList()[1];
                    }
                    html.Append("<tr><td <td style='border: 1px solid black;padding:5px' valign=top>" + item["GroupName"].ToString() + "</td><td <td style='border: 1px solid black;padding:5px' valign=top>" + approver + "</td><td <td style='border: 1px solid black;padding:5px' valign=top>" + item["ApprovalStatus"].ToString() + "</td><td <td style='border: 1px solid black;padding:5px' valign=top>" + Comments + "</td>");
                }
                html.Append("</table></div>");
            }
            return html.ToString();
        }
        /// <summary>
        /// beautifies json 
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public string SyntaxHighlightJson(string original)
        {
            return Regex.Replace(
              original,
              @"(¤(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\¤])*¤(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)".Replace('¤', '"'),
              match =>
              {
                  var cls = "number";
                  if (Regex.IsMatch(match.Value, @"^¤".Replace('¤', '"')))
                  {
                      if (Regex.IsMatch(match.Value, ":$"))
                      {
                          cls = "key";
                      }
                      else
                      {
                          cls = "string";
                      }
                  }
                  else if (Regex.IsMatch(match.Value, "true|false"))
                  {
                      cls = "boolean";
                  }
                  else if (Regex.IsMatch(match.Value, "null"))
                  {
                      cls = "null";
                  }
                  return "<span class=\"" + cls + "\">" + match + "</span>";
              });
        }
        /// <summary>
        /// Replaces $$ values
        /// </summary>
        /// <param name="InputJson"></param>
        /// <param name="FormName"></param>
        /// <param name="requestid"></param>
        /// <returns></returns>
        public string ReplaceInputJsonValues(string InputJson, string FormName, string requestid)
        {
            try
            {
                string JsonString = InputJson;
                string value = string.Empty;
                DataTable FormFieldValues = obj.GetFormValue(Session["programid"].ToString(), requestid);
                DataTable RequestDetails = obj.GetRequest(requestid, Session["programid"].ToString());
                DataTable dtKeyValue = obj.GetKeyValueRepository(string.Empty);
                var lstMatches = Regex.Matches(JsonString, @"\*\*[^\*]*\*\*");
                bool IsReplaced = false;
                string attachments = string.Empty;
                int count = 0;
                string FileName_WithoutExtension = string.Empty;
                foreach (var item in lstMatches)
                {
                    try
                    {
                        IsReplaced = false;
                        foreach (DataRow dr in dtKeyValue.Rows)
                        {
                            if (item.ToString().Substring(11, item.ToString().Substring(11).IndexOf(')')).ToLower() == dr["Key"].ToString().ToLower())
                            {
                                DataTable dtval = obj.GetKeyValueRepository(dr["Id"].ToString());
                                InputJson = InputJson.Replace(item.ToString(), dtval.Rows[0]["Value"].ToString());
                                IsReplaced = true;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                    if (!IsReplaced)
                        InputJson = InputJson.Replace("**GetValue(" + item.ToString() + ")**", string.Empty);
                }
                lstMatches = Regex.Matches(JsonString, @"\$\$[^\$]*\$\$");
                Dictionary<string, string> DictStepOutputs = new Dictionary<string, string>();
                DataTable dtAutomationStepsValues = obj.GetAutomationStepsValuesForRequest(requestid, Session["programid"].ToString());
                if (dtAutomationStepsValues != null && dtAutomationStepsValues.Rows.Count > 0)
                {
                    foreach (DataRow dr in dtAutomationStepsValues.Rows)
                    {
                        try
                        {
                            DictStepOutputs.Add(dr["BlockName"].ToString() + dr["StepName"].ToString() + "[" + (string.IsNullOrEmpty(dr["LoopIndex"].ToString()) ? "0" : dr["LoopIndex"].ToString()) + "]", dr["StepOutput"].ToString());
                        }
                        catch (Exception ex) { log.Error(ex.Message); }
                    }
                }
                foreach (var item in lstMatches)
                {
                    try
                    {
                        if (item.ToString().ToLower().Contains("block"))
                        {
                            if (DictStepOutputs.Any(c => c.Key.ToLower().Contains(item.ToString().ToLower().Replace("$$", "").Split('_')[0] + item.ToString().ToLower().Replace("$$", "").Split('_')[1])))
                            {
                                JObject Output = JObject.Parse(DictStepOutputs.First(c => c.Key.ToLower().Contains(item.ToString().ToLower().Replace("$$", "").Split('_')[0] + item.ToString().ToLower().Replace("$$", "").Split('_')[1])).Value);
                                try
                                {
                                    value = Output.SelectToken(item.ToString().Replace("$$", "").Split('_')[2]).ToString().Replace(Environment.NewLine, "");
                                    if (value.StartsWith("{"))
                                    {
                                        if (!value.Contains("["))
                                        {
                                            value = value.Replace("{", "[");
                                            value = value.Replace("}", "]");
                                        }
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                    }
                                    else if (value.StartsWith("[") && value.EndsWith("]"))
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), value);
                                    }
                                    else
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                }
                                catch (Exception)
                                {
                                    value = "['" + string.Join("','", Output.SelectTokens(item.ToString().ToLower().Replace("$$", "").Split('_')[2]).ToList()) + "']";
                                    try
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                    }
                                    catch (Exception)
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), "-unable to parse-");
                                    }
                                }
                            }
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("requestid"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["requestid"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("formname"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), FormName.Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("description"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["description"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("summary") || item.ToString().ToLower().Trim().Replace("$$", "").Equals("short_description"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["summary"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("category"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["category"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("createdby") || item.ToString().ToLower().Trim().Replace("$$", "").Equals("caller_id"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["createdby"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("assignment_group") || item.ToString().ToLower().Trim().Replace("$$", "").Equals("assignmentgroup"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["assignmentgroup	"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("opened_at") || item.ToString().ToLower().Trim().Replace("$$", "").Equals("CreatedDate"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["CreatedDate"].ToString());
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("sys_id") || item.ToString().ToLower().Trim().Replace("$$", "").Equals("sysid"))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["sysid"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (item.ToString().ToLower().Trim().Replace("$$", "").Equals("attachments") || item.ToString().ToLower().Trim().Replace("$$", "").Contains("attachments"))
                        {
                            DataTable dt_attachments = obj.GetRequestAttachments(RequestDetails.Rows[0]["sysid"].ToString());
                            if (item.ToString().ToLower().Trim().Replace("$$", "").Contains("attachments["))
                            {
                                try
                                {
                                    Regex regex = new Regex("[(.*)]");
                                    var v = regex.Match(item.ToString());
                                    int index = int.Parse(item.ToString().Substring(14, 1));
                                    attachments = Manager.GetConfiguration("Attachment.DownloadAPI") + dt_attachments.Rows[index]["GUID"].ToString();
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                }
                            }
                            else
                            {
                                attachments = attachments + "[";
                                foreach (DataRow dr in dt_attachments.Rows)
                                {
                                    try
                                    {
                                        if (count == 0)
                                        {
                                            attachments = attachments + "'" + Manager.GetConfiguration("Attachment.DownloadAPI") + dr["GUID"].ToString() + "'";
                                            count++;
                                        }
                                        else
                                        {
                                            attachments = attachments + "," + "'" + Manager.GetConfiguration("Attachment.DownloadAPI") + dr["GUID"].ToString() + "'";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error(ex.Message);
                                    }
                                }
                                attachments = attachments + "]";
                            }
                            InputJson = InputJson.Replace(item.ToString(), attachments.Replace(@"\", @"\\").ToString());
                        }
                        else if (item.ToString().ToLower().Contains("parameters"))
                        {
                            if (item.ToString().ToLower().Contains("parameters_"))
                            {
                                JObject Output = JObject.Parse(RequestDetails.Rows[0]["description"].ToString());
                                try
                                {
                                    value = Output.SelectToken(item.ToString().Replace("$$", "").Split('_')[1].Replace(Environment.NewLine, "")).ToString();
                                    if (value.StartsWith("{"))
                                    {
                                        if (!value.Contains("["))
                                        {
                                            value = value.Replace("{", "[");
                                            value = value.Replace("}", "]");
                                        }
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                    }
                                    else if (value.StartsWith("[") && value.EndsWith("]"))
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                    }
                                    else
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                }
                                catch (Exception)
                                {
                                    value = "['" + string.Join("','", Output.SelectTokens(item.ToString().Replace("$$", "").Split('_')[1]).ToList()) + "']";
                                    try
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), value.Replace(@"\", @"\\"));
                                    }
                                    catch (Exception)
                                    {
                                        InputJson = InputJson.Replace(item.ToString(), "-unable to parse-");
                                    }
                                }
                            }
                            else
                                InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0]["description"].ToString().Replace(@"\", @"\\"));
                        }
                        else if (RequestDetails.Columns.Contains(item.ToString().Trim().Replace("$$", "")))
                        {
                            InputJson = InputJson.Replace(item.ToString(), RequestDetails.Rows[0][item.ToString().Trim().Replace("$$", "")].ToString().Replace(@"\", @"\\"));
                        }
                        else
                        {
                            foreach (DataRow dr in FormFieldValues.Rows)
                            {
                                if (item.ToString().ToLower().Trim().Replace("$$", "").Equals(dr["fieldname"].ToString().ToLower()))
                                {
                                    InputJson = InputJson.Replace(item.ToString(), dr["fieldvalue"].ToString().Replace(@"\", @"\\"));
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        log.Error("error while parsing--" + ee.Message);
                    }
                }
            }
            catch (Exception ee)
            {
                log.Error("error while parsing--" + ee.Message);
            }
            return InputJson.Trim();
        }
        /// <summary>
        /// method to submit user response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btn_Click(object sender, EventArgs e)
        {
            SaveStepDetails();
        }
        /// <summary>
        /// saves input in db
        /// </summary>
        public void SaveStepDetails()
        {
            try
            {
                DataTable dt = null;
                dt = obj.getRequestData(Session["sys"].ToString());
                if (dt.Rows.Count.Equals(0))
                {
                    Session["message_title"] = "MESSAGE INFO";
                    Session["message"] = "No inputs needed ";
                    Session["message_type"] = "Info";
                    ShowMessage();
                    return;
                }
                string status = dt.Rows[0]["status"].ToString();
                int formid = 0;
                string requestid = string.Empty;
                DataTable objtable = null;
                string value = string.Empty;
                StringBuilder sb = new StringBuilder();
                string progress_comments = string.Empty;
                string Approvers = string.Empty;
                string approversList = string.Empty;
                string SuggestedFormID = string.Empty;
                switch (status)
                {
                    #region l2
                    case "SENTFORL2":
                        Approvers = dt.Rows[0]["l2approver"].ToString();
                        string approvalstatus = string.Empty;
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        bool isl2 = false;
                        IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), true, out GroupName, out approversList);
                        isl2 = isapprover;
                        if (isl2 == true)
                        {
                            RadioButtonList approvalrdr = newForm.FindControl("rdrapproval") as RadioButtonList;
                            if (approvalrdr.SelectedIndex == 0)
                            {
                                SuggestedFormID = formid.ToString();
                                approvalstatus = "Approved";
                                objtable = obj.GetFormFields(formid.ToString());
                                obj.DeleteFormValues(formid.ToString(), requestid);
                                foreach (DataRow row in objtable.Rows)
                                {
                                    if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                                    {
                                        string type = row["fieldtype"].ToString();
                                        switch (type)
                                        {
                                            case "TEXTBOX":
                                            case "DATE":
                                                TextBox txt = newForm.FindControl("txt_" + row["fieldid"].ToString()) as TextBox;
                                                value = txt.Text;
                                                if (value != "")
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString(), value);
                                                else
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                                break;
                                            case "DROPDOWN":
                                                DropDownList ddl = newForm.FindControl("ddl_" + row["fieldid"].ToString()) as DropDownList;
                                                value = ddl.SelectedIndex != 0 ? ddl.SelectedItem.ToString() : null;
                                                if (value != null)
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString(), value);
                                                else
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                                break;
                                            case "CHECKBOXLIST":
                                                CheckBoxList cbl = newForm.FindControl("cbl_" + row["fieldid"].ToString()) as CheckBoxList;
                                                value = "";
                                                if (cbl.Items != null && cbl.Items.Count > 0)
                                                {
                                                    foreach (ListItem item in cbl.Items)
                                                    {
                                                        if (item.Selected == true)
                                                            value = value + item.Value + ",";
                                                    }
                                                }
                                                if (value != null)
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString(), value);
                                                else
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                                break;
                                            case "RADIO":
                                                RadioButtonList rdr = newForm.FindControl("radio_" + row["fieldid"].ToString()) as RadioButtonList;
                                                
                                                value = rdr.SelectedValue;
                                                obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString(), value);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(hdnValue.Value))
                                        {
                                            dynamic json = JsonConvert.DeserializeObject(hdnValue.Value);
                                            try
                                            {
                                                value = json[row["fieldname"].ToString()];
                                                if (!string.IsNullOrEmpty(value))
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString(), value);
                                                else
                                                    obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                            }
                                            catch (Exception)
                                            {
                                                obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                            }
                                        }
                                        else
                                        {
                                            obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                        }
                                        
                                    }
                                }
                                value = "SENDTOUSER";
                                progress_comments = "Approved by L2 approver";
                                obj.IncrementMapSuccess(formid.ToString(), Session["programid"].ToString());
                            }
                            else if (approvalrdr.SelectedIndex == 1)
                            {
                                SuggestedFormID = formid.ToString();
                                approvalstatus = "Remapped";
                                obj.IncrementMapFailure(formid.ToString(), Session["programid"].ToString());
                                DropDownList ddl = newForm.FindControl("drpdwnforms") as DropDownList;
                                string val = ddl.SelectedIndex != 0 ? ddl.SelectedValue.ToString() : null;
                                if (val != null)
                                {
                                    obj.UpdateRequestFormMap(requestid, Session["programid"].ToString(), val);
                                    value = "SENDTOUSER";
                                    progress_comments = "L2 remapped form to " + ddl.SelectedItem;
                                    formid = int.Parse(val);
                                    objtable = obj.GetFormFields(formid.ToString());
                                    foreach (DataRow row in objtable.Rows)
                                    {
                                        obj.insertFormValues(formid.ToString(), requestid, row["fieldid"].ToString(), row["fieldname"].ToString());
                                    }
                                }
                                else
                                {
                                    value = "NOMAPPING";
                                    progress_comments = "L2 could not map any form";
                                }
                            }
                            else
                            {
                                SuggestedFormID = formid.ToString();
                                approvalstatus = "L2Handle";
                                value = "L2HANDLE";
                                progress_comments = "Request will be manually handled by L2 approver";
                            }
                            obj.UpdateInfopathMailer(formid.ToString(), value, requestid, Session["programid"].ToString(),SuggestedFormID);
                            obj.UpdateRequest(progress_comments, Session["sys"].ToString(), value);
                           
                            string comment = Session["mail"].ToString() + ":" + progress_comments;
                           
                            newForm.Controls.Clear();
                            SRAFDBConnection.Utils.StatusMessage msg = obj.UpdateApprovalHistory(comment, Session["programid"].ToString(), requestid);
                            switch (msg.MsgType)
                            {
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-success'><strong>" + msg.Message + "<br/><a href='pendingactions.aspx'>Click Here</a> to see all Pending Actions." + "</strong></div>"));
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Error:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-warning'><strong>" + msg.Message + "</strong></div>"));
                                   
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Exception:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-danger'><strong>" + msg.Message + "</strong></div>"));
                                    
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this approval.";
                            Session["message_type"] = "Danger";
                            ShowMessage();
                            return;
                        }
                        break;
                    #endregion
                    case "SENTTOUSER":
                        #region user
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        bool isuser = false;
                        if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                            isuser = true;
                        if (isuser == true)
                        {
                            objtable = obj.GetFormFields(formid.ToString());
                            foreach (DataRow row in objtable.Rows)
                            {
                                if (dt.Rows[0]["formtype"].ToString().ToLower() == "normal")
                                {
                                    string type = row["fieldtype"].ToString();
                                    switch (type)
                                    {
                                        case "TEXTBOX":
                                        case "DATE":
                                            TextBox txt = newForm.FindControl("txt_" + row["fieldid"].ToString()) as TextBox;
                                            value = txt.Text;
                                            if (value != "")
                                                obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                            else { }
                                            break;
                                        case "DROPDOWN":
                                            DropDownList ddl = newForm.FindControl("ddl_" + row["fieldid"].ToString()) as DropDownList;
                                            if ((Boolean)row["isMandatory"] == true)
                                            {
                                                if (ddl.SelectedIndex != 0)
                                                {
                                                    value = ddl.SelectedItem.ToString();
                                                    obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                                }
                                                else
                                                {
                                                    Session["message"] = "Please select " + row["fieldname"].ToString();
                                                    Session["message_title"] = "Required";
                                                    Session["message_type"] = "Danger";
                                                    ShowMessage();
                                                    return;
                                                }
                                            }
                                            else
                                            {
                                                value = ddl.SelectedIndex != 0 ? ddl.SelectedItem.ToString() : null;
                                                if (value != null)
                                                    obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                            }
                                            break;
                                        case "CHECKBOXLIST":
                                            try
                                            {
                                                CheckBoxList cbl = newForm.FindControl("cbl_" + row["fieldid"].ToString()) as CheckBoxList;
                                                value = "";
                                                if (row["fieldname"].ToString() == "GROUP")//if multiple approval
                                                {
                                                    string approvalString = "";
                                                    if (cbl.Items != null && cbl.Items.Count > 0)
                                                    {
                                                        foreach (ListItem item in cbl.Items)
                                                        {
                                                            if (item.Selected == true)
                                                            {
                                                                value = value + item.Value + ",";
                                                                approvalString = approvalString + "NA,";
                                                            }
                                                        }
                                                    }
                                                    if (value != null)
                                                        obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid, approvalString);
                                                }
                                                else
                                                {
                                                    if (cbl.Items != null && cbl.Items.Count > 0)
                                                    {
                                                        foreach (ListItem item in cbl.Items)
                                                        {
                                                            if (item.Selected == true)
                                                            {
                                                                value = value + item.Value + ",";
                                                            }
                                                        }
                                                    }
                                                    if ((Boolean)row["isMandatory"] == true)
                                                    {
                                                        if (value != "")
                                                            obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                                        else
                                                        {
                                                            Session["message"] = "Please select atleast one value for " + row["fieldname"].ToString();
                                                            Session["message_title"] = "Required";
                                                            Session["message_type"] = "Danger";
                                                            ShowMessage();
                                                            return;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                                    }
                                                }
                                            }
                                            catch (Exception ee)
                                            {
                                                log.Error(ee.Message);
                                                log.Error(ee.StackTrace);
                                            }
                                            break;
                                        case "RADIO":
                                            RadioButtonList rdr = newForm.FindControl("radio_" + row["fieldid"].ToString()) as RadioButtonList;
                                            try
                                            {
                                                value = rdr.SelectedValue;
                                                if (value == null)
                                                    value = "";
                                                if ((Boolean)row["isMandatory"] == true)
                                                {
                                                    if (value != "")
                                                        obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                                    else
                                                    {
                                                        Session["message"] = "Please select " + row["fieldname"].ToString();
                                                        Session["message_title"] = "Required";
                                                        Session["message_type"] = "Danger";
                                                        ShowMessage();
                                                        return;
                                                    }
                                                }
                                                else
                                                {
                                                    obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                                }
                                            }
                                            catch (Exception ee)
                                            {
                                                log.Error(ee.Message);
                                                log.Error(ee.StackTrace);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(hdnValue.Value))
                                    {
                                        dynamic json = JsonConvert.DeserializeObject(hdnValue.Value);
                                        try
                                        {
                                            value = json[row["fieldname"].ToString()];
                                            if (!string.IsNullOrEmpty(value))
                                                obj.UpdateFormValue(value, row["fieldid"].ToString(), requestid);
                                        }
                                        catch (Exception ex)
                                        {
                                            log.Error("error while getting value for fieldname--" + row["fieldname"].ToString());
                                            log.Error(ex.Message);
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                            }
                            progress_comments = "User responded to the mail";
                            obj.UpdateInfopathMailerForuser("SENDFORAPPROVAL", requestid, Session["programid"].ToString());
                            obj.UpdateUserInputHistory(requestid, Session["programid"].ToString(), Session["mail"].ToString(), "UserMoreInfo", 0);
                            string stateid = obj.getstateid(Session["programid"].ToString(), "inprogress");                    
                            SRAFDBConnection.Utils.StatusMessage msg = obj.UpdateRequestForUser(progress_comments, Session["sys"].ToString(), stateid);
                            newForm.Controls.Clear();
                            switch (msg.MsgType)
                            {
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-success'><strong>" + msg.Message + "<br/><a href='pendingactions.aspx'>Click here</a> to see all pending actions." + "</strong></div>"));
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Error:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-warning'><strong>" + msg.Message + "</strong></div>"));
                                   
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Exception:
                                   
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-danger'><strong>" + msg.Message + "</strong></div>"));
                                   
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this approval.";
                            Session["message_type"] = "Danger";
                            ShowMessage();
                            return;
                        }
                        break;
                        #endregion
                    case "SENTFORAPPROVAL":
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORAPPROVAL":
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERCONFIRMATION":
                        #region approver
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        isapprover = false;
                        string reason = string.Empty;
                        DataTable exchangedetails = obj.GetGlobalParameters(Session["programid"].ToString());
                        try
                        {
                            log.Debug("trying to get all form fields for formid-" + formid);
                            objtable = obj.GetFormFields(formid.ToString());
                            log.Info("successfully got all info for form fields for formid--" + formid);
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while getting all form fields for formid--" + ex.Message);
                            if (ex.InnerException != null)
                            {
                                log.Error(ex.InnerException.Message);
                            }
                        }
                        log.Debug("trying to comapre session mailid with approver in db");
                        string groupName = string.Empty;
                        IsApprover(dt, out isapprover, out IsApproved, Session["mail"].ToString(), false, out groupName, out approversList);
                        if (isapprover == true && !IsApproved)
                        {
                            RadioButtonList rdrapproval = newForm.FindControl("rdrapproval") as RadioButtonList;
                            TextBox group = newForm.FindControl("txt_GROUP") as TextBox;
                            bool isExceptionalApprover = false;
                            try
                            {
                                isExceptionalApprover = Session["isExceptionalApprover"] != null ? (Convert.ToBoolean(Session["isExceptionalApprover"])) : false;
                                if (Session["l2Approver"] != null && isExceptionalApprover == false)
                                {
                                    if (Session["l2Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                    {
                                        isExceptionalApprover = true;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                isExceptionalApprover = false;
                            }
                            if (status == "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORAPPROVAL" || status == "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERCONFIRMATION")
                            {
                                DataTable dtStep = obj.GetLastStepDetails(requestid, Session["programid"].ToString());
                                DataTable dtApprovals = obj.GetAutomationApprovalHistory(Session["programid"].ToString(), requestid, int.Parse(dtStep.Rows[0]["StepID"].ToString()));
                                foreach (DataRow drApproval in dtApprovals.Rows)
                                {
                                    if (drApproval["ApprovalStatus"].ToString().ToLower().Contains("pending") && (drApproval["Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()) || isExceptionalApprover))
                                    {
                                        if (rdrapproval.SelectedIndex == 0)
                                        {
                                            TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                            txtreason.Visible = false;
                                            Label lbl = newForm.FindControl("labelreason") as Label;
                                            lbl.Text = "";
                                            progress_comments = dtStep.Rows[0]["AliasName"].ToString() + " : " + drApproval["GroupName"].ToString() + " : approved by " + Session["mail"].ToString() + ". ";
                                            obj.UpdateInfopathMailerDate(requestid, "", Session["programid"].ToString());
                                            obj.UpdateRequestForMultilevel(progress_comments, Session["sys"].ToString());
                                            string ApprovalComment = string.Empty;
                                            if (txtreason != null && !string.IsNullOrEmpty(txtreason.Text))
                                                ApprovalComment = Session["mail"].ToString() + ":" + txtreason.Text;
                                            else
                                                ApprovalComment = Session["mail"].ToString() + ":";
                                            obj.UpdateApprovalForAutomationStep("Approved", requestid, Session["programid"].ToString(), ApprovalComment, drApproval["GroupName"].ToString(), int.Parse(dtStep.Rows[0]["StepID"].ToString()));
                                            try
                                            {
                                                log.Debug("moving mail to other approvers");
                                                if (!string.IsNullOrEmpty(drApproval["Approver"].ToString()) && drApproval["Approver"].ToString().Contains(","))
                                                {
                                                    List<string> approvers = new List<string>();
                                                    approvers = drApproval["Approver"].ToString().Split(',').ToList();
                                                    MailType = "Approval Notification to Other Approvers";
                                                    foreach (string s in approvers)
                                                    {
                                                        if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                        {
                                                            obj.SendMail("Request #" + drApproval["RequestID"].ToString() + " Approval", "Request #" + drApproval["RequestID"].ToString() + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " Group/ Level-" + drApproval["GroupName"].ToString() : "") + " has been approved by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " and Group/ Level-" + drApproval["GroupName"].ToString() : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while sending mail to other approvers-" + ex.Message);
                                            }
                                        }
                                        else if (rdrapproval.SelectedIndex == 1)
                                        {
                                            TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                            txtreason.Visible = false;
                                            Label lbl = newForm.FindControl("labelreason") as Label;
                                            lbl.Text = "";
                                            reason = txtreason.Text;
                                            progress_comments = dtStep.Rows[0]["AliasName"].ToString() + " : " + drApproval["GroupName"].ToString() + " : rejected by " + Session["mail"].ToString() + ". ";
                                            obj.UpdateInfopathMailerDate(requestid, reason, Session["programid"].ToString());
                                            obj.UpdateRequestForMultilevel(progress_comments, Session["sys"].ToString());
                                            string ApprovalComment = string.Empty;
                                            ApprovalComment = Session["mail"].ToString() + ":" + reason;
                                            obj.UpdateApprovalForAutomationStep("Rejected", requestid, Session["programid"].ToString(), ApprovalComment, drApproval["GroupName"].ToString(), int.Parse(dtStep.Rows[0]["StepID"].ToString()));
                                            try
                                            {
                                                log.Debug("moving mail to other approvers");
                                                if (!string.IsNullOrEmpty(drApproval["Approver"].ToString()) && drApproval["Approver"].ToString().Contains(","))
                                                {
                                                    List<string> approvers = new List<string>();
                                                    approvers = drApproval["Approver"].ToString().Split(',').ToList();
                                                    MailType = "Approval Notification to Other Approvers";
                                                    foreach (string s in approvers)
                                                    {
                                                        if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                        {
                                                            obj.SendMail("Request #" + drApproval["RequestID"].ToString() + " Rejection", "Request #" + drApproval["RequestID"].ToString() + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " Group/ Level-" + drApproval["GroupName"].ToString() : "") + " has been rejected by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " and Group/ Level-" + drApproval["GroupName"].ToString() : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while sending mail to other approvers-" + ex.Message);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else if (group != null && !string.IsNullOrEmpty(group.Text))//if group access request
                            {
                                DataTable fielddata = obj.getfielddata("GROUP", requestid);
                                if (fielddata != null && fielddata.Rows != null && fielddata.Rows.Count > 0 && objtable != null && objtable.Rows != null && objtable.Rows.Count > 0)
                                {
                                    List<string> groups = fielddata.Rows[0]["fieldvalue"].ToString().Split(',').ToList();
                                    List<string> approvals = fielddata.Rows[0]["fieldvalueapproval"].ToString().Split(',').ToList();
                                    foreach (var gp in groups)
                                    {
                                        if (group.Text == gp)
                                        {
                                            int index = groups.IndexOf(gp);
                                            if (approvals[index] == "NA")
                                            {
                                                if (rdrapproval.SelectedIndex == 0)
                                                {
                                                    approvals[index] = "TRUE";
                                                }
                                                else
                                                {
                                                    approvals[index] = "FALSE";
                                                }
                                                string approvalString = string.Join(",", approvals);
                                                obj.UpdateFormValueForApproval(approvalString, requestid);
                                                if (!approvalString.Contains("NA"))
                                                {
                                                    string ApprovalComment = string.Empty;
                                                    if (rdrapproval.SelectedIndex == 0)
                                                    {
                                                        TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                                        txtreason.Visible = false;
                                                        Label lbl = newForm.FindControl("labelreason") as Label;
                                                        lbl.Text = "";
                                                        if (txtreason != null && !string.IsNullOrEmpty(txtreason.Text))
                                                            ApprovalComment = Session["mail"].ToString() + ":" + txtreason.Text;
                                                        else
                                                            ApprovalComment = Session["mail"].ToString() + ":";
                                                        progress_comments = "Request for access to " + group.Text + " group is approved by " + Session["mail"].ToString() + ". ";
                                                        obj.UpdateApprovalHistoryForApproval("Approved", ApprovalComment, Session["programid"].ToString(), requestid, group.Text);
                                                        try
                                                        {
                                                            log.Debug("moving mail to other approvers");
                                                            if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                                            {
                                                                List<string> approvers = new List<string>();
                                                                approvers = approversList.Split(',').ToList();
                                                                MailType = "Approval Notification to Other Approvers";
                                                                foreach (string s in approvers)
                                                                {
                                                                    if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                                    {
                                                                        obj.SendMail("Request #" + requestid + " Approval", "Request #" + requestid + (!string.IsNullOrEmpty(group.Text) ? " Group/ Level-" + group.Text : "") + " has been approved by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(group.Text) ? " and Group/ Level-" + group.Text : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            log.Error("error while sending mail to other approvers-" + ex.Message);
                                                        }
                                                    }
                                                    else if (rdrapproval.SelectedIndex == 1)
                                                    {
                                                        TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                                        txtreason.Visible = false;
                                                        Label lbl = newForm.FindControl("labelreason") as Label;
                                                        lbl.Text = "";
                                                        reason = txtreason.Text;
                                                        ApprovalComment = Session["mail"].ToString() + ":" + reason;
                                                        progress_comments = "Request for access to " + group.Text + " group is rejected by " + Session["mail"].ToString() + " with reason : " + reason + ". ";
                                                        obj.UpdateApprovalHistoryForApproval("Rejected", ApprovalComment, Session["programid"].ToString(), requestid, group.Text);
                                                        try
                                                        {
                                                            log.Debug("moving mail to other approvers");
                                                            if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                                            {
                                                                List<string> approvers = new List<string>();
                                                                approvers = approversList.Split(',').ToList();
                                                                MailType = "Approval Notification to Other Approvers";
                                                                foreach (string s in approvers)
                                                                {
                                                                    if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                                    {
                                                                        obj.SendMail("Request #" + requestid + " Rejection", "Request #" + requestid + (!string.IsNullOrEmpty(group.Text) ? " Group/ Level-" + group.Text : "") + " has been rejected by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(group.Text) ? " and Group/ Level-" + group.Text : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            log.Error("error while sending mail to other approvers-" + ex.Message);
                                                        }
                                                    }
                                                    if (approvalString.Contains("TRUE") && approvalString.Contains("FALSE"))
                                                    {
                                                        string approvedgroups = string.Empty;
                                                        List<string> approvallist = approvalString.Split(',').ToList();
                                                        foreach (var appr in approvallist)
                                                        {
                                                            if (appr == "TRUE")
                                                            {
                                                                approvedgroups += groups[approvallist.IndexOf(appr)];
                                                            }
                                                        }
                                                        progress_comments += "Request is partially approved for groups : " + approvedgroups + ". ";
                                                        obj.UpdateInfopathMailerForApproval("FORMAPPROVED", requestid, "", Session["programid"].ToString());
                                                        obj.UpdateRequest(progress_comments, Session["sys"].ToString(), "");
                                                    }
                                                    else if (approvalString.Contains("TRUE"))
                                                    {
                                                        progress_comments += "Request is approved. ";
                                                        obj.UpdateInfopathMailerForApproval("FORMAPPROVED", requestid, "", Session["programid"].ToString());
                                                        obj.UpdateRequest(progress_comments, Session["sys"].ToString(), "");
                                                    }
                                                    else if (approvalString.Contains("FALSE"))
                                                    {
                                                        progress_comments += "Request is rejected. ";
                                                        obj.UpdateInfopathMailerForApproval("FORMREJECTED", requestid, reason, Session["programid"].ToString());
                                                        string stateid = obj.getstateid(Session["programid"].ToString(), "closed");
                                                        obj.UpdateRequestForApproval(progress_comments, Session["sys"].ToString(), stateid);
                                                    }
                                                }
                                                else
                                                {
                                                    if (rdrapproval.SelectedIndex == 0)
                                                    {
                                                        TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                                        txtreason.Visible = false;
                                                        Label lbl = newForm.FindControl("labelreason") as Label;
                                                        lbl.Text = "";
                                                        progress_comments = "Request for access to " + group.Text + " group is approved by " + Session["mail"].ToString() + ". ";
                                                        obj.UpdateInfopathMailerDate(requestid, "", Session["programid"].ToString());
                                                        obj.UpdateRequestForUser(progress_comments, Session["sys"].ToString(), "0");
                                                        string ApprovalComment = string.Empty;
                                                        if (txtreason != null && !string.IsNullOrEmpty(txtreason.Text))
                                                            ApprovalComment = Session["mail"].ToString() + ":" + txtreason.Text;
                                                        else
                                                            ApprovalComment = Session["mail"].ToString() + ":";
                                                        obj.UpdateApprovalHistoryForApproval("Approved", ApprovalComment, Session["programid"].ToString(), requestid, group.Text);
                                                        try
                                                        {
                                                            log.Debug("moving mail to other approvers");
                                                            if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                                            {
                                                                List<string> approvers = new List<string>();
                                                                approvers = approversList.Split(',').ToList();
                                                                MailType = "Approval Notification to Other Approvers";
                                                                foreach (string s in approvers)
                                                                {
                                                                    if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                                    {
                                                                        obj.SendMail("Request #" + requestid + " Approval", "Request #" + requestid + (!string.IsNullOrEmpty(group.Text) ? " Group/ Level-" + group.Text : "") + " has been approved by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(group.Text) ? " and Group/ Level-" + group.Text : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            log.Error("error while sending mail to other approvers-" + ex.Message);
                                                        }
                                                    }
                                                    else if (rdrapproval.SelectedIndex == 1)
                                                    {
                                                        TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                                        txtreason.Visible = false;
                                                        Label lbl = newForm.FindControl("labelreason") as Label;
                                                        lbl.Text = "";
                                                        reason = txtreason.Text;
                                                        progress_comments = "Request for access to " + group.Text + " group is rejected by " + Session["mail"].ToString() + " due to following reason : " + reason + ". ";
                                                        obj.UpdateInfopathMailerDate(requestid, reason, Session["programid"].ToString());
                                                        obj.UpdateRequestForUser(progress_comments, Session["sys"].ToString(), "0");
                                                        string ApprovalComment = string.Empty;
                                                        ApprovalComment = Session["mail"].ToString() + ":" + reason;
                                                        obj.UpdateApprovalHistoryForApproval("Rejected", ApprovalComment, Session["programid"].ToString(), requestid, group.Text);
                                                        try
                                                        {
                                                            log.Debug("moving mail to other approvers");
                                                            if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                                            {
                                                                List<string> approvers = new List<string>();
                                                                approvers = approversList.Split(',').ToList();
                                                                MailType = "Approval Notification to Other Approvers";
                                                                foreach (string s in approvers)
                                                                {
                                                                    if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                                    {
                                                                        obj.SendMail("Request #" + requestid + " Rejection", "Request #" + requestid + (!string.IsNullOrEmpty(group.Text) ? " Group/ Level-" + group.Text : "") + " has been rejected by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(group.Text) ? " and Group/ Level-" + group.Text : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            log.Error("error while sending mail to other approvers-" + ex.Message);
                                                        }
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (dt.Rows[0]["formapprovaltype"].ToString() == "5")//if multilevel approval
                            {
                                DataTable dtApprovals = obj.GetMultiLevelApprovalHistory(Session["programid"].ToString(), requestid);
                                foreach (DataRow drApproval in dtApprovals.Rows)
                                {
                                    if (drApproval["ApprovalStatus"].ToString().ToLower().Contains("pending") && (drApproval["Approver"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()) || isExceptionalApprover))
                                    {
                                        if (rdrapproval.SelectedIndex == 0)
                                        {
                                            TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                            txtreason.Visible = false;
                                            Label lbl = newForm.FindControl("labelreason") as Label;
                                            lbl.Text = "";
                                            progress_comments = dtApprovals.Rows[0]["GroupName"].ToString() + " : approved by " + Session["mail"].ToString() + ". ";
                                            obj.UpdateInfopathMailerDate(requestid, "", Session["programid"].ToString());
                                            obj.UpdateRequestForMultilevel(progress_comments, Session["sys"].ToString());
                                            string ApprovalComment = string.Empty;
                                            if (txtreason != null && !string.IsNullOrEmpty(txtreason.Text))
                                                ApprovalComment = Session["mail"].ToString() + ":" + txtreason.Text;
                                            else
                                                ApprovalComment = Session["mail"].ToString() + ":";
                                            obj.UpdateApprovalForMultilevel("Approved", requestid, Session["programid"].ToString(), ApprovalComment, dtApprovals.Rows[0]["GroupName"].ToString());
                                            try
                                            {
                                                log.Debug("moving mail to other approvers");
                                                if (!string.IsNullOrEmpty(drApproval["Approver"].ToString()) && drApproval["Approver"].ToString().Contains(","))
                                                {
                                                    List<string> approvers = new List<string>();
                                                    approvers = drApproval["Approver"].ToString().Split(',').ToList();
                                                    MailType = "Approval Notification to Other Approvers";
                                                    foreach (string s in approvers)
                                                    {
                                                        if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                        {
                                                            obj.SendMail("Request #" + drApproval["RequestID"].ToString() + " Approval", "Request #" + drApproval["RequestID"].ToString() + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " Group/ Level-" + drApproval["GroupName"].ToString() : "") + " has been approved by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " and Group/ Level-" + drApproval["GroupName"].ToString() : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while sending mail to other approvers-" + ex.Message);
                                            }
                                        }
                                        else if (rdrapproval.SelectedIndex == 1)
                                        {
                                            TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                            txtreason.Visible = false;
                                            Label lbl = newForm.FindControl("labelreason") as Label;
                                            lbl.Text = "";
                                            reason = txtreason.Text;
                                            progress_comments = dtApprovals.Rows[0]["GroupName"].ToString() + " : rejected by " + Session["mail"].ToString() + ". ";
                                            obj.UpdateInfopathMailerDate(requestid, reason, Session["programid"].ToString());
                                            obj.UpdateRequestForMultilevel(progress_comments, Session["sys"].ToString());
                                            string ApprovalComment = string.Empty;
                                            ApprovalComment = Session["mail"].ToString() + ":" + reason;
                                            obj.UpdateApprovalForMultilevel("Rejected", requestid, Session["programid"].ToString(), ApprovalComment, dtApprovals.Rows[0]["GroupName"].ToString());
                                            try
                                            {
                                                log.Debug("moving mail to other approvers");
                                                if (!string.IsNullOrEmpty(drApproval["Approver"].ToString()) && drApproval["Approver"].ToString().Contains(","))
                                                {
                                                    List<string> approvers = new List<string>();
                                                    approvers = drApproval["Approver"].ToString().Split(',').ToList();
                                                    MailType = "Approval Notification to Other Approvers";
                                                    foreach (string s in approvers)
                                                    {
                                                        if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                        {
                                                            obj.SendMail("Request #" + drApproval["RequestID"].ToString() + " Rejection", "Request #" + drApproval["RequestID"].ToString() + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " Group/ Level-" + drApproval["GroupName"].ToString() : "") + " has been rejected by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(drApproval["GroupName"].ToString()) ? " and Group/ Level-" + drApproval["GroupName"].ToString() : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                log.Error("error while sending mail to other approvers-" + ex.Message);
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                string ApprovalComment = string.Empty;
                                if (rdrapproval.SelectedIndex == 0)
                                {
                                    value = "FORMAPPROVED";
                                    TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                    txtreason.Visible = false;
                                    Label lbl = newForm.FindControl("labelreason") as Label;
                                    lbl.Text = "";
                                    progress_comments = "Request is approved. ";
                                    if (txtreason != null && !string.IsNullOrEmpty(txtreason.Text))
                                        ApprovalComment = Session["mail"].ToString() + ":" + txtreason.Text;
                                    else
                                        ApprovalComment = Session["mail"].ToString() + ":";
                                }
                                else
                                {
                                    TextBox txtreason = newForm.FindControl("txtreason") as TextBox;
                                    txtreason.Visible = false;
                                    Label lbl = newForm.FindControl("labelreason") as Label;
                                    lbl.Text = "";
                                    value = "FORMREJECTED";
                                    reason = txtreason.Text;
                                    progress_comments = "Request is rejected stating reason--" + reason + ". ";
                                }
                                if (value == "FORMREJECTED")
                                {
                                    obj.UpdateInfopathMailerForApproval(value, requestid, reason, Session["programid"].ToString());
                                    string stateid = obj.getstateid(Session["programid"].ToString(), "closed");
                                    obj.UpdateRequestForApproval(progress_comments, Session["sys"].ToString(), stateid);
                                    ApprovalComment = Session["mail"].ToString() + ":" + reason;
                                    obj.UpdateApprovalForFormApproval("Rejected", requestid, Session["programid"].ToString(), ApprovalComment);
                                    try
                                    {
                                        log.Debug("moving mail to other approvers");
                                        if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                        {
                                            List<string> approvers = new List<string>();
                                            approvers = approversList.Split(',').ToList();
                                            MailType = "Approval Notification to Other Approvers";
                                            foreach (string s in approvers)
                                            {
                                                if (s.ToLower() != Session["mail"].ToString().ToLower())
                                                {
                                                    obj.SendMail("Request #" + requestid + " Rejection", "Request #" + requestid + (!string.IsNullOrEmpty(groupName) ? " Group/ Level-" + groupName : "") + " has been rejected by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(groupName) ? " and Group/ Level-" + groupName : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error("error while sending mail to other approvers-" + ex.Message);
                                    }
                                }
                                if (value == "FORMAPPROVED")
                                {
                                    obj.UpdateInfopathMailerForApproval(value, requestid, "", Session["programid"].ToString());
                                    obj.UpdateRequestForMultilevel(progress_comments, Session["sys"].ToString());
                                    obj.UpdateApprovalForFormApproval("Approved", requestid, Session["programid"].ToString(), ApprovalComment);
                                    try
                                    {
                                        log.Debug("moving mail to other approvers");
                                        if (!string.IsNullOrEmpty(approversList) && approversList.Contains(","))
                                        {
                                            List<string> approvers = new List<string>();
                                            approvers = approversList.Split(',').ToList();
                                            MailType = "Approval Notification to Other Approvers";
                                            foreach (string s in approvers)
                                            {
                                                if (s.ToLower() != Session["mail"].ToString())
                                                {
                                                    obj.SendMail("Request #" + requestid + " Approval", "Request #" + requestid + (!string.IsNullOrEmpty(groupName) ? " Group/ Level-" + groupName : "") + " has been approved by " + Session["mail"].ToString() + (!string.IsNullOrEmpty(reason) ? "<br/>Comment:" + reason : "") + ". <br/>Further email responses for this request" + (!string.IsNullOrEmpty(groupName) ? " and Group/ Level-" + groupName : "") + " won't be processed.", s, true, exchangedetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Error("error while sending mail to other approvers-" + ex.Message);
                                    }
                                }
                            }
                            newForm.Controls.Clear();
                            SRAFDBConnection.Utils.StatusMessage msg = new SRAFDBConnection.Utils.StatusMessage();
                            msg.Message = "Your response has been submitted. Thank You. <br/><a href='pendingactions.aspx'>Click here</a> to see all pending actions.";
                            msg.MsgType = SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info;
                            switch (msg.MsgType)
                            {
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-success'><strong>" + msg.Message + "</strong></div>"));
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Error:
                                   
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-warning'><strong>" + msg.Message + "</strong></div>"));
                                    
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Exception:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-danger'><strong>" + msg.Message + "</strong></div>"));
                                    
                                    break;
                        #endregion
                                default:
                                    break;
                            }
                        }
                        else if (IsApproved)
                        {
                            Session["message_title"] = "No inputs needed";
                            Session["message"] = "Looks like one of the approver has given all the inputs that we needed. Incase we need any further information / approval, we’ll send you an email.";
                            Session["message_type"] = "Info";
                            ShowMessage();
                            return;
                        }
                        else
                        {
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this approval.";
                            Session["message_type"] = "Danger";
                            ShowMessage();
                            return;
                        }
                        break;
                    case "FORMAPPROVED-AUTOMATIONSTARTED-SENTFORUSERINPUT":
                        formid = Convert.ToInt32(dt.Rows[0]["formid"].ToString());
                        requestid = dt.Rows[0]["requestid"].ToString();
                        bool authorizedToAccess = false;
                        if (Session["mail"].ToString().ToLower().Equals(dt.Rows[0]["createdby"].ToString().ToLower()))
                            isuser = true;
                        DataTable dtLastStepValues = obj.GetLastStepDetails(Session["requestid"].ToString(), Session["programid"].ToString());
                        try
                        {
                            log.Debug("trying to comapre session mailid with user in db");
                            if (dtLastStepValues.Rows[0]["Stepinput"].ToString().ToLower().Contains(Session["mail"].ToString().ToLower()))
                                authorizedToAccess = true;
                        }
                        catch (Exception ex)
                        {
                            log.Error("error while comparing session mailid with user-" + ex.Message);
                            authorizedToAccess = false;
                        }
                        if (authorizedToAccess == true)
                        {
                            string UserInput = string.Empty;
                            if (!string.IsNullOrEmpty(hdnValue.Value))
                            {
                                JObject Output = JObject.Parse(hdnValue.Value);
                                Output.Add("IsDataCollected", true);
                                JObject UserDetails = new JObject();
                                UserDetails["ADID"] = Session["uid"].ToString();
                                UserDetails["Name"] = Session["name"].ToString();
                                UserDetails["MailID"] = Session["mail"].ToString();
                                Output.Add("UserDetails", UserDetails);
                                UserInput = Output.ToString();
                            }
                            else
                            {
                                JObject Output = new JObject();
                                Output.Add("IsDataCollected", true);
                                JObject UserDetails = new JObject();
                                UserDetails["ADID"] = Session["uid"].ToString();
                                UserDetails["Name"] = Session["name"].ToString();
                                UserDetails["MailID"] = Session["mail"].ToString();
                                Output.Add("UserDetails", UserDetails);
                                UserInput = Output.ToString();
                            }
                            if (dtLastStepValues != null && dtLastStepValues.Rows.Count > 0)
                            {
                                obj.UpdateAutomationStepValues(int.Parse(dtLastStepValues.Rows[0]["StepID"].ToString()), Session["requestid"].ToString(), UserInput, true, 0, 0, "");
                                obj.UpdateUserInputHistory(requestid, Session["programid"].ToString(), Session["mail"].ToString(), "UserInput", int.Parse(dtLastStepValues.Rows[0]["StepID"].ToString()));
                            }
                            progress_comments = "User responded to the mail";
                            obj.UpdateInfopathMailerForuser("FORMAPPROVED-RESUMEAUTOMATION", requestid, Session["programid"].ToString());
                            string stateid = obj.getstateid(Session["programid"].ToString(), "inprogress");
                            SRAFDBConnection.Utils.StatusMessage msg = obj.UpdateRequestForUser(progress_comments, Session["sys"].ToString(), stateid);
                            newForm.Controls.Clear();
                            switch (msg.MsgType)
                            {
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-success'><strong>" + msg.Message + "<br/><a href='pendingactions.aspx'>Click Here</a> to see all Pending Actions." + "</strong></div>"));
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Error:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-warning'><strong>" + msg.Message + "</strong></div>"));
                                    
                                    break;
                                case SRAFDBConnection.Utils.StatusMessage.MsgLevel.Exception:
                                    
                                    newForm.Controls.Add(new LiteralControl("<div class='alert alert-danger'><strong>" + msg.Message + "</strong></div>"));
                                   
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            Session["message_title"] = "Unauthorized";
                            Session["message"] = "Not authorized for this request.";
                            Session["message_type"] = "Danger";
                            ShowMessage();
                            return;
                        }
                        break;
                    default:
                        Session["message_title"] = "No inputs needed";
                        Session["message"] = "Looks like one of the approver has given all the inputs that we needed. Incase we need any further information / approval, we’ll send you an email.";
                        Session["message_type"] = "Info";
                        ShowMessage();
                        return;
                }
            }
            catch (Exception ee)
            {
                log.Error("Error while submitting approval. " + ee.Message);
                log.Error(ee.StackTrace);
            }
            GetPendingActions();
            InactivateAll();
            requestFormDetailsLi.Attributes.Add("class", "active");
            requestFormDetailsLi.Attributes.Add("class", "tab-pane active");
            newForm.Attributes.Add("class", "active");
            newForm.Attributes.Add("class", "tab-pane active");
        }
        /// <summary>
        /// checks for approvers
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="IsApprover"></param>
        /// <param name="IsApproved"></param>
        /// <param name="loggedinuser"></param>
        /// <param name="IsL2"></param>
        /// <param name="Group"></param>
        public void IsApprover(DataTable dt, out bool IsApprover, out bool IsApproved, string loggedinuser, bool IsL2, out string Group, out string approversList)
        {
            IsApprover = false;
            IsApproved = true;
            Group = string.Empty;
            approversList = "";
            try
            {
                string Delegates = string.Empty;
                DataTable dtApprovals = obj.GetApprovalHistory(dt.Rows[0]["programid"].ToString(), dt.Rows[0]["requestid"].ToString(), IsL2);
                foreach (DataRow dr in dtApprovals.Rows)
                {
                    Delegates = obj.GetActiveDelegatesForApprovers(dr["Approver"].ToString());
                    if (dr["Approver"].ToString().ToLower().Contains(loggedinuser.ToLower()) || Delegates.ToLower().Contains(loggedinuser.ToLower()))
                    {
                        IsApprover = true;
                        if (dr["ApprovalStatus"].ToString().ToLower().Contains("pending"))
                        {
                            IsApproved = false;
                            Group = dr["GroupName"].ToString();
                            approversList = dr["Approver"].ToString();
                            break;
                        }
                    }
                    else if (HttpContext.Current.Session["isExceptionalApprover"] != null)
                    {
                        if (Convert.ToBoolean(HttpContext.Current.Session["isExceptionalApprover"]) == true)
                        {
                            IsApprover = true;
                            if (dr["ApprovalStatus"].ToString().ToLower().Contains("pending"))
                            {
                                IsApproved = false;
                                Group = dr["GroupName"].ToString();
                                approversList = dr["Approver"].ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Error while checking for approvers--" + e.Message);
            }
        }
        /// <summary>
        /// checks if its multilevel 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="IsApprover"></param>
        /// <param name="IsApproved"></param>
        /// <param name="loggedinuser"></param>
        public void IsMultiLevelApprover(DataTable dt, out bool IsApprover, out bool IsApproved, string loggedinuser)
        {
            IsApprover = false;
            IsApproved = false;
            DataTable dtApprovals = obj.GetMultiLevelApprovalHistory(dt.Rows[0]["programid"].ToString(), dt.Rows[0]["requestid"].ToString());
            foreach (DataRow dr in dtApprovals.Rows)
            {
                if (dr["Approver"].ToString().ToLower().Contains(loggedinuser.ToLower()))
                {
                    IsApprover = true;
                    if (!dr["ApprovalStatus"].ToString().ToLower().Contains("pending"))
                        IsApproved = true;
                    break;
                }
            }
        }
        /// <summary>
        /// Gets all pending actions
        /// </summary>
        private void GetPendingActions()
        {
            try
            {
                DataTable dtActions = obj.GetActionsHistory(Session["mail"].ToString(), "", "", "", false, "me");
                if (dtActions != null && dtActions.Rows.Count > 0)
                {
                    countAction = dtActions.Rows.Count.ToString();
                    int countreq = 0;
                    try
                    {
                        countreq = Convert.ToInt32(countAction);
                        if (countreq < 1000)
                        {
                            countAction = countreq.ToString();
                        }
                        else if (countreq > 1000000)
                        {
                            decimal countreqn = countreq / 1000000;
                            countAction = Math.Truncate(Truncate(countreqn, 1)).ToString() + "M";
                        }
                        else if (countreq > 999)
                        {
                            decimal countreqn = Convert.ToDecimal(countreq / 1000.0);
                            countAction = (Truncate(countreqn, 1)).ToString() + "K";
                        }
                        else
                        {
                            countAction = countreq.ToString();
                        }
                    }
                    catch (Exception)
                    {
                        countAction = dtActions.Rows.Count.ToString();
                    }
                }
                else
                {
                    countAction = "0";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while getting actions-" + ex.Message);
                countAction = "0";
            }
            try
            {
                string delsysids = "";
                DataTable dtPending = obj.GetPendingActions(Session["mail"].ToString(), "", false, "me", "", "", out delsysids);
                if (dtPending != null && dtPending.Rows.Count > 0)
                {
                    countPending = dtPending.Rows.Count.ToString();
                    int countreq = 0;
                    try
                    {
                        countreq = Convert.ToInt32(countPending);
                        if (countreq < 1000)
                        {
                            countPending = countreq.ToString();
                        }
                        else if (countreq > 1000000)
                        {
                            decimal countreqn = countreq / 1000000;
                            countPending = Math.Truncate(Truncate(countreqn, 1)).ToString() + "M";
                        }
                        else if (countreq > 999)
                        {
                            decimal countreqn = Convert.ToDecimal(countreq / 1000.0);
                            countPending = (Truncate(countreqn, 1)).ToString() + "K";
                        }
                        else
                        {
                            countPending = countreq.ToString();
                        }
                    }
                    catch (Exception)
                    {
                        countAction = dtPending.Rows.Count.ToString();
                    }
                }
                else
                {
                    countPending = "0";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while getting actions-" + ex.Message);
                countPending = "0";
            }
        }
        /// <summary>
        /// Truncates decimal number
        /// </summary>
        /// <param name="number"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public decimal Truncate(decimal number, int digits)
        {
            decimal stepper = (decimal)(Math.Pow(10.0, (double)digits));
            int temp = (int)(stepper * number);
            return (decimal)temp / stepper;
        }
        /// <summary>
        /// Gets approver list
        /// </summary>
        public void GetApprovalList()
        {
            try
            {
                DataTable dt = obj.GetApprovers(Session["requestid"].ToString(), Session["programid"].ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    gvApproverList.Visible = true;
                    gvApproverList.DataSource = dt;
                    gvApproverList.DataBind();
                    foreach (GridViewRow gv in gvApproverList.Rows)
                    {
                        try
                        {
                            if (gv.Cells[3].Text.Contains(":"))
                            {
                                gv.Cells[2].Text = gv.Cells[3].Text.Split(':')[0];
                                gv.Cells[3].Text = gv.Cells[3].Text.Split(':')[1];
                                if (string.IsNullOrEmpty(gv.Cells[3].Text))
                                {
                                    gv.Cells[3].Text = "---";
                                }
                            }
                            else
                            {
                                gv.Cells[2].Text = "---";
                                gv.Cells[3].Text = "---";
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                        try
                        {
                            if ((!string.IsNullOrEmpty(gv.Cells[4].Text)) && gv.Cells[4].Text != "N/A")
                            {
                                gv.Cells[4].Text = (Convert.ToDateTime(gv.Cells[4].Text)).ToString("dd-MMM-yyyy HH:mm");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }
                }
                else
                {
                    gvApproverList.Visible = false;
                    lblData.Text = "<div class='callout callout-info'><h4>No Approval History</h4>No approval history available</div>";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while showing approval data--" + ex.Message);
                gvApproverList.Visible = false;
                lblData.Text = "<div class='callout callout-info'><h4>No Approval History</h4>No approval history available</div>";
            }
        }
        /// <summary>
        /// Gets mail history
        /// </summary>
        public void GetMailHistory()
        {
            try
            {
                DataTable dt = obj.GetMailHistory(Session["requestid"].ToString(), Session["programid"].ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    gvMailList.Visible = true;
                    gvMailList.DataSource = dt;
                    gvMailList.DataBind();
                    foreach (GridViewRow gv in gvMailList.Rows)
                    {
                        try
                        {
                            gv.Cells[3].Text = HttpUtility.HtmlDecode(gv.Cells[3].Text.Replace("1px solid black", "1px solid white"));
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                        try
                        {
                            if ((!string.IsNullOrEmpty(gv.Cells[4].Text)) && gv.Cells[4].Text != "N/A")
                            {
                                gv.Cells[4].Text = (Convert.ToDateTime(gv.Cells[4].Text)).ToString("dd-MMM-yyyy HH:mm");
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }
                }
                else
                {
                    gvMailList.Visible = false;
                    lblMailHistory.Text = "<div class='callout callout-info'><h4>No Mail History</h4>No mail history available</div>";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while showing mail data--" + ex.Message);
                gvMailList.Visible = false;
                lblMailHistory.Text = "<div class='callout callout-info'><h4>No Mail History</h4>No mail history available</div>";
            }
        }
        /// <summary>
        /// Automation progress click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnAutomationProgress_Click(object sender, EventArgs e)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop1", "showhidetest();", true);
            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop", "window.open('" + ConfigurationManager.AppSettings["srafUrl"].ToString() + "?id=" + ApprovalWorkflowApp.Utils.Crypto.Encrypt(Session["requestid"].ToString()) + "&pid=" + ApprovalWorkflowApp.Utils.Crypto.Encrypt(Session["programid"].ToString()) + "')", true);
        }
        /// <summary>
        /// Gets work notes
        /// </summary>
        public void GetComments()
        {
            try
            {
                lblUploadInfo.Text = "- Comments can't be empty and should be more than 10 characters<br />- Comments cannot have special characters < or ><br/>- Allowed Format for file upload: " + ConfigurationManager.AppSettings["FileExtensions"].ToString() + " <br/>- Max file size- " + ConfigurationManager.AppSettings["FileSize"].ToString() + "Mb";
                DataTable dt = obj.GetCommentHistory(Session["sys"].ToString());
                if (dt != null && dt.Rows.Count > 0)
                {
                    countNotes = dt.Rows.Count.ToString();
                    StringBuilder sb = new StringBuilder();
                    string lastUsed = string.Empty;
                    foreach (DataRow dr in dt.Rows)
                    {
                        try
                        {
                            string initials = "";
                            List<string> firstChars = dr["UserName"].ToString().Split(' ').ToList();
                            foreach (string s in firstChars)
                            {
                                initials = initials + s.Substring(0, 1);
                            }
                            
                            if (dt.Rows.IndexOf(dr) == 0)
                            {
                                lastUsed = "firstRow";
                                sb.Append("<div style='width:100%'><div class='avatarfirstRow'><span id='fontstyling'>" + initials + "</span></div><div class='firstRow RowStyling' > <div style='width:100%' > <span class='pull-right time'><i style='margin-right:5px;' class='fa fa-clock-o'></i>" + (Convert.ToDateTime(dr["CommentDate"].ToString())).ToString("dd-MMM-yyyy HH:mm") + "</span> <h5  style='color: #2285b3; font-weight: 700;'><span style='margin-right:5px;' class='fa fa-user'></span>" + dr["UserName"].ToString() + " (" + dr["UserADID"].ToString() + ")</h5> <span >" + dr["CommentText"].ToString() + "</span> </div>");
                            }
                            else if (dt.Rows[dt.Rows.IndexOf(dr) - 1]["EmailID"].ToString().ToLower() == dr["Emailid"].ToString().ToLower())
                            {
                                if (lastUsed == "nextRow")
                                {
                                    lastUsed = "nextRow";
                                    sb.Append("<div style='width:100%'><div class='avatarnextRow'><span id='fontstyling'>" + initials + "</span></div><div class='nextRow RowStyling'> <div  style='width:100%'> <span class='pull-right time'><i style='margin-right:5px;' class='fa fa-clock-o'></i>" + (Convert.ToDateTime(dr["CommentDate"].ToString())).ToString("dd-MMM-yyyy HH:mm") + "</span> <h5  style='color: #2285b3; font-weight: 700;'><span style='margin-right:5px;' class='fa fa-user'></span>" + dr["UserName"].ToString() + " (" + dr["UserADID"].ToString() + ")</h5> <span >" + dr["CommentText"].ToString() + "</span> </div>");
                                }
                                else
                                {
                                    lastUsed = "firstRow";
                                    sb.Append("<div style='width:100%'><div class='avatarfirstRow'><span id='fontstyling'>" + initials + "</span></div><div class='firstRow RowStyling'> <div  style='width:100%'> <span class='pull-right time'><i style='margin-right:5px;' class='fa fa-clock-o'></i>" + (Convert.ToDateTime(dr["CommentDate"].ToString())).ToString("dd-MMM-yyyy HH:mm") + "</span> <h5  style='color: #2285b3; font-weight: 700;'><span style='margin-right:5px;' class='fa fa-user'></span>" + dr["UserName"].ToString() + " (" + dr["UserADID"].ToString() + ")</h5> <span >" + dr["CommentText"].ToString() + "</span> </div>");
                                }
                            }
                            else
                            {
                                if (lastUsed == "nextRow")
                                {
                                    lastUsed = "firstRow";
                                    sb.Append("<div style='width:100%'><div class='avatarfirstRow'><span id='fontstyling'>" + initials + "</span></div><div class='firstRow RowStyling'  > <div   style='width:100%' > <span class='pull-right time'><i style='margin-right:5px;' class='fa fa-clock-o'></i>" + (Convert.ToDateTime(dr["CommentDate"].ToString())).ToString("dd-MMM-yyyy HH:mm") + "</span> <h5  style='color: #2285b3; font-weight: 700;'><span style='margin-right:5px;' class='fa fa-user'></span>" + dr["UserName"].ToString() + " (" + dr["UserADID"].ToString() + ")</h5> <span >" + dr["CommentText"].ToString() + "</span> </div>");
                                }
                                else
                                {
                                    lastUsed = "nextRow";
                                    sb.Append("<div style='width:100%'><div class='avatarnextRow'><span id='fontstyling'>" + initials + "</span></div><div class='nextRow RowStyling' > <div   style='width:100%'> <span class='pull-right time'><i style='margin-right:5px;' class='fa fa-clock-o'></i>" + (Convert.ToDateTime(dr["CommentDate"].ToString())).ToString("dd-MMM-yyyy HH:mm") + "</span> <h5  style='color: #2285b3; font-weight: 700;'><span style='margin-right:5px;' class='fa fa-user'></span>" + dr["UserName"].ToString() + " (" + dr["UserADID"].ToString() + ")</h5> <span >" + dr["CommentText"].ToString() + "</span> </div>");
                                }
                            }
                            if (!string.IsNullOrEmpty(dr["FileName"].ToString()))
                            {
                                sb.Append("<span>" + "Attachment: <a href='DownloadAttachment.ashx?id=" + Utils.Crypto.Encrypt(dr["ID"].ToString()) + "' target='_blank'>" + dr["FileName"].ToString() + "</a></span>");
                            }
                            sb.Append("</div></div>");
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                    }
                    lblWorkNotes.Text = sb.ToString();
                }
                else
                {
                    countNotes = "0";
                    lblWorkNotes.Text = "<div class='callout callout-danger'>No notes available</div>";
                }
            }
            catch (Exception ex)
            {
                log.Error("Error while showing approval data--" + ex.Message);
                lblWorkNotes.Text = "<div class='callout callout-danger'>No notes available</div>";
            }
        }
        /// <summary>
        /// Click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Unnamed_ServerClick(object sender, EventArgs e)
        {
            InactivateAll();
            existingFormLI.Attributes.Add("class", "active");
            existingFormLI.Attributes.Add("class", "tab-pane active");
            existingForm.Attributes.Add("class", "active");
            existingForm.Attributes.Add("class", "tab-pane active");
            SRAFDBConnection.Utils.StatusMessage msg = new SRAFDBConnection.Utils.StatusMessage();
            ScriptManager.RegisterStartupScript(this, this.GetType(), "Pop1", "showhidetest();", true);
            if (txtChatComments.Text.Trim().Length < 10)
            {
                lblErrorText.Text = "Comments can't be empty and should be more than 10 characters";
                return;
            }
            else
            {
                lblErrorText.Text = "";
            }
            if (txtChatComments.Text.Trim().Contains("<") || txtChatComments.Text.Trim().Contains(">"))
            {
                lblErrorText.Text = "Comments cannot have special characters < or >";
                return;
            }
            else
            {
                lblErrorText.Text = "";
            }
            string attachment = string.Empty;
            string filename = string.Empty;
            if (fileUploadAttachment.HasFile)
            {
                bool isvalid = false;
                string extension = Path.GetExtension(fileUploadAttachment.FileName);
                List<string> extensions = ConfigurationManager.AppSettings["FileExtensions"].ToString().Split(',').ToList();
                foreach (string s in extensions)
                {
                    if (s.ToLower().Trim() == extension.ToLower().Trim())
                    {
                        isvalid = true;
                        break;
                    }
                }
                if (!isvalid)
                {
                    lblErrorText.Text = "Allowed extensions for file upload are- " + ConfigurationManager.AppSettings["FileExtensions"].ToString();
                    return;
                }
                else
                {
                    lblErrorText.Text = "";
                }
                int size = fileUploadAttachment.PostedFile.ContentLength;
                if (size > (Convert.ToInt32(ConfigurationManager.AppSettings["FileSize"].ToString()) * 1024 * 1024))
                {
                    lblErrorText.Text = "Allowed size for file upload is- " + ConfigurationManager.AppSettings["FileSize"].ToString() + " Mb.";
                    return;
                }
                else
                {
                    lblErrorText.Text = "";
                }
                byte[] file = null;
                Stream fs = fileUploadAttachment.PostedFile.InputStream;
                BinaryReader br = new BinaryReader(fs);
                file = br.ReadBytes((Int32)fs.Length);
                br.Close();
                fs.Close();
                br.Dispose();
                fs.Dispose();
                attachment = Convert.ToBase64String(file);
                filename = Path.GetFileName(fileUploadAttachment.FileName);
            }
            try
            {
                try
                {
                    msg = obj.InsertComments(Session["sys"].ToString(), Session["mail"].ToString(), attachment, txtChatComments.Text, filename, Session["name"].ToString(), Session["uid"].ToString());
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
                if (msg.MsgType == SRAFDBConnection.Utils.StatusMessage.MsgLevel.Info)
                {
                    try
                    {
                        List<string> recievers = new List<string>();
                        string mailTo = string.Empty;
                        DataTable dtapprovalHistory = obj.GetApprovalHistory(Session["programid"].ToString(), Session["requestid"].ToString(), false);
                        if (dtapprovalHistory != null && dtapprovalHistory.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dtapprovalHistory.Rows)
                            {
                                
                                if (!string.IsNullOrEmpty(dr["Comment"].ToString()))
                                {
                                    recievers.Add(dr["Comment"].ToString().Split(':')[0]);
                                }
                                else
                                {
                                    if (dr["Approver"].ToString().Contains(","))
                                    {
                                        List<string> newList = dr["Approver"].ToString().Split(',').ToList();
                                        foreach (string s in newList)
                                        {
                                            if (!string.IsNullOrEmpty(s))
                                                recievers.Add(s);
                                        }
                                    }
                                    else
                                    {
                                        recievers.Add(dr["Approver"].ToString());
                                    }
                                }
                            }
                        }
                        if (Session["l2Approver"] != null)
                        {
                            if (Session["l2Approver"].ToString().Contains(","))
                            {
                                List<string> newList = Session["l2Approver"].ToString().Split(',').ToList();
                                foreach (string s in newList)
                                {
                                    if (!string.IsNullOrEmpty(s))
                                        recievers.Add(s);
                                }
                            }
                            else
                                recievers.Add(Session["l2Approver"].ToString());
                        }
                        try
                        {
                            DataTable conversationHistory = obj.GetCommentHistory(Session["sys"].ToString());
                            if (conversationHistory != null && conversationHistory.Rows.Count > 0)
                            {
                                foreach (DataRow dr in conversationHistory.Rows)
                                {
                                    recievers.Add(dr["EmailID"].ToString());
                                }
                            }
                            else
                            {
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex.Message);
                        }
                        DataTable dtDetails = obj.GetGlobalParametersForMail(Session["programid"].ToString(), Session["requestid"].ToString());
                        if (dtDetails != null && dtDetails.Rows.Count > 0)
                        {
                            recievers.Add(dtDetails.Rows[0]["createdby1"].ToString());
                        }
                        bool mailsent = false;
                        if (recievers != null && recievers.Count > 0)
                        {
                            recievers = recievers.Distinct().ToList();
                            string To = string.Join(",", recievers.ToArray());
                            MailType = "New Note Notification";
                            mailsent = obj.SendMail("#" + Session["requestid"].ToString() + " New Note Added ", "A new note has been added to request #" + Session["requestid"].ToString() + " by " + Session["name"].ToString() + ".<br/>Note:" + txtChatComments.Text + " <br/><a href='" + ConfigurationManager.AppSettings["SrafApprovalurl"].ToString() + "?sys=" + ApprovalWorkflowApp.Utils.Crypto.Encrypt(Session["sys"].ToString()) + "&mode=exist'>Click Here</a> to view the note.", To, true, dtDetails, MailPriority.High,"","",Session["programid"].ToString(), Session["requestid"].ToString(),MailType);
                        }
                        if (mailsent)
                        {
                        }
                        else
                        {
                            lblErrorText.Text = "Work notes updated but we were not able to notify due to technical reasons.";
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error while sending mail-" + ex.Message);
                        lblErrorText.Text = "Work notes updated but we were not able to notify due to technical reasons.";
                    }
                }
                else
                {
                    lblErrorText.Text = "Error while updating work notes. Please try again later.";
                }
                GetComments();
                txtChatComments.Text = "";
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }
        
        /// <summary>
        /// inactivates all tab
        /// </summary>
        private void InactivateAll()
        {
            newFormLI.Attributes.Clear();
            newFormLI.Attributes.Add("class", "");
            newForm.Attributes.Clear();
            newForm.Attributes.Add("class", "tab-pane");
            existingFormLI.Attributes.Clear();
            existingFormLI.Attributes.Add("class", "");
            existingForm.Attributes.Clear();
            existingForm.Attributes.Add("class", "tab-pane");
            existingForm.Attributes.Add("style", "margin-top:4%");
            requestFormDetailsLi.Attributes.Clear();
            requestFormDetailsLi.Attributes.Add("class", "");
            newForm.Attributes.Clear();
            newForm.Attributes.Add("class", "tab-pane");
            newForm.Attributes.Add("style", "margin-top:4%");
            automationLi.Attributes.Clear();
            automationLi.Attributes.Add("class", "");
            automationProgress.Attributes.Clear();
            automationProgress.Attributes.Add("class", "tab-pane");
            automationProgress.Attributes.Add("style", "margin-top:4%");
            ApprovalLi.Attributes.Clear();
            ApprovalLi.Attributes.Add("class", "");
            approvalHistory.Attributes.Clear();
            approvalHistory.Attributes.Add("class", "tab-pane");
            approvalHistory.Attributes.Add("style", "margin-top:4%");
            MailLi.Attributes.Clear();
            MailLi.Attributes.Add("class", "");
            mailHistory.Attributes.Clear();
            mailHistory.Attributes.Add("class", "tab-pane");
            mailHistory.Attributes.Add("style", "margin-top:4%");
        }
        /// <summary>
        /// Checks authorization
        /// </summary>
        /// <returns></returns>
        private bool CheckAuthorization()
        {
            bool isauthorized = false;
            try
            {
                bool isadmin = false;
                DataTable dt = obj.GetAllRolesWithGroup(Session["uid"].ToString(),out isadmin);
                if (dt != null && dt.Rows.Count > 0)
                {
                    if (isadmin)
                    {
                        isauthorized = true;
                        Session["isExceptionalApprover"] = true;
                    }
                    else
                    {
                        Session["isExceptionalApprover"] = false;
                        foreach (DataRow dr in dt.Rows)
                        {
                            if (dr["programid"].ToString().ToLower().Trim() == Session["programid"].ToString().ToLower().Trim())
                            {
                                isauthorized = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    isauthorized = false;
                    Session["isExceptionalApprover"] = false;
                }
            }
            catch (Exception ex)
            {
                Session["isExceptionalApprover"] = false;
                log.Error("Error while checking authorization-" + ex.Message);
            }
            return isauthorized;
        }
        /// <summary>
        /// Binds approver data row data bound
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvApproverList_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if(e.Row.RowType==DataControlRowType.DataRow)
            {
                try
                {
                    if (!string.IsNullOrEmpty(e.Row.Cells[0].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "")))
                    {
                        string approvers = e.Row.Cells[0].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "");
                        string newapprovers = string.Empty;
                        if(e.Row.Cells[0].Text.Contains(","))
                        {
                            List<string> owners = approvers.Split(',').ToList();
                            foreach(string st in owners)
                            {
                                if(!string.IsNullOrEmpty(st))
                                    newapprovers = newapprovers + "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + st + "</span></span><br/>";
                            }
                        }
                        else
                        {
                            newapprovers = "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + approvers + "</span></span>";
                        }
                        e.Row.Cells[0].Text = newapprovers;
                    }
                    if(!string.IsNullOrEmpty(e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "")))
                    {
                        switch(e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "").ToLower())
                        {
                            case "approved":
                                e.Row.Cells[1].Text = "<span class='fa fa-thumbs-up' style='color:#5cb85c;font-size:25px;'></span>";
                                e.Row.Cells[1].ToolTip = "Approved";
                                break;
                            case "rejected":
                                e.Row.Cells[1].Text = "<span class='fa fa-thumbs-down' style='color:#d9534f;font-size:25px;'></span>";
                                e.Row.Cells[1].ToolTip = "Rejected";
                                break;
                            default:
                                e.Row.Cells[1].Text = "<span class='fa fa-hourglass-half' style='color:#5BBFDE;font-size:25px;'></span>";
                                e.Row.Cells[1].ToolTip = "Pending";
                                break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                }
        }
    }
        /// <summary>
        /// Binds mail data row data bound
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvMailList_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if(e.Row.RowType==DataControlRowType.DataRow)
            {
                string audienceText = string.Empty;
                try
                {
                    if (!string.IsNullOrEmpty(e.Row.Cells[0].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "")))
                    {
                        audienceText = "<b>To:</b><br/>";
                        if(e.Row.Cells[0].Text.Contains(","))
                        {
                            List<string> owners = e.Row.Cells[0].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "").Split(',').ToList();
                            foreach (string st in owners)
                            {
                                if (!string.IsNullOrEmpty(st))
                                    audienceText = audienceText + "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + st + "</span></span><br/>";
                            }
                        }
                        else
                        {
                            audienceText = audienceText + "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + e.Row.Cells[0].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "") + "</span></span>";
                        }
                    }
                    if (!string.IsNullOrEmpty(e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "")))
                    {
                        audienceText =audienceText+ "<b>Cc:</b><br/>";
                        if (e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "").Contains(","))
                        {
                            List<string> owners = e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "").Split(',').ToList();
                            foreach (string st in owners)
                            {
                                if (!string.IsNullOrEmpty(st))
                                    audienceText = audienceText + "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + st + "</span></span><br/>";
                            }
                        }
                        else
                        {
                            audienceText = audienceText + "<span class='fa fa-user' style='color:darkblue'> <span style='padding-left:6px;color:black;'>" + e.Row.Cells[1].Text.Replace("&amp;", "").Replace("amp;", "").Replace("&nbsp;", "").Replace("nbsp;", "") + "</span></span>";
                        }
                    }
                    e.Row.Cells[0].Text = audienceText;
                }
                catch(Exception ex)
                {
                    log.Error(ex.Message);
                }
            }
        }
    }
}