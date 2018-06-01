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
    /// Action history page
    /// </summary>
    public partial class ActionHistory : System.Web.UI.Page
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
            message_div.Visible = false;
            if (!IsPostBack)
            {
                BindGrid();
            }
        }
        /// <summary>
        /// Binds the data to grid
        /// </summary>
        private void BindGrid()
        {
            SQLServerConnection obj = new SQLServerConnection();
            string column = string.Empty;
            bool isAdmin = false;
            string filtertext = string.Empty;
            if (ddlColumns.SelectedIndex != 0)
                column = ddlColumns.SelectedValue;
            filtertext = txtSearchTerm.Text;
            try
            {
                DataTable dtaccess = obj.GetAllAccountsWithGroup(Session["uid"].ToString(),out isAdmin);
                if (isAdmin)
                {
                    ddlResults.Visible = true;
                }
            }
            catch (Exception ex)
            {
                log.Error(String.Format("Error while checking admin-{0}",ex.Message));
                isAdmin = false;
            }
            if (isAdmin == false)
                ddlResults.Visible = false;
            DataTable dt = obj.GetActionsHistory(Session["mail"].ToString(), ddlFilterRequests.SelectedValue,column,filtertext,isAdmin,ddlResults.SelectedValue);
            if (dt != null && dt.Rows.Count > 0)
            {
                gvOpenRequest.Columns[0].Visible = true;
                gvOpenRequest.Visible = true;
                gvOpenRequest.DataSource = dt;
                gvOpenRequest.DataBind();
                gvOpenRequest.Columns[0].Visible = false;
            }
            else
            {
                gvOpenRequest.Visible = false;
                Session["message_title"] = "No Requests";
                Session["message"] = "Currently no action history available.";
                Session["message_type"] = "Info";
                ShowMessage();
            }
        }
        /// <summary>
        /// Shows message
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
                message_div.Visible = false;
            }
        }
        /// <summary>
        /// Selected index change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void rdrResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid();
        }
        /// <summary>
        /// Row data bound event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvOpenRequest_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                try
                {
                    string href = String.Format("approval.aspx?sys={0}",Crypto.Encrypt(e.Row.Cells[0].Text));
                    e.Row.Cells[1].Text = String.Format("<u><a href='{0}' target='_blank' title='This is a link to the Request Form' class='info'>{1}</a></u>",href, e.Row.Cells[1].Text);
                }
                catch (Exception ex)
                {
                    log.Error(String.Format("Error while setting href for requestid--{0}",ex.Message));
                }
                try
                {
                    if(e.Row.Cells[3].Text.Length>30)
                    {
                        e.Row.Cells[3].ToolTip=e.Row.Cells[3].Text;
                        e.Row.Cells[3].Text=e.Row.Cells[3].Text.Substring(0,29);
                    }
                    else
                    {
                        e.Row.Cells[3].ToolTip = e.Row.Cells[3].Text;
                    }
                }
                catch(Exception ex)
                {
                    log.Error(String.Format("Error-{0}",ex.Message));
                }
            }
        }
        /// <summary>
        /// Page index change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gvOpenRequest_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            gvOpenRequest.PageIndex = e.NewPageIndex;
            BindGrid();
        }
        /// <summary>
        /// filter dropdown change event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ddlFilterRequests_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindGrid();
        }
        /// <summary>
        /// Filter button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnFilterForSearch_Click(object sender, EventArgs e)
        {
            BindGrid();
        }
        /// <summary>
        /// clear filter click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnClearFilter_Click(object sender, EventArgs e)
        {
            ddlColumns.SelectedIndex = 0;
            txtSearchTerm.Text = "";
            BindGrid();
        }
    }
}