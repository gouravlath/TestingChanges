using ApprovalWorkflowApp.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// Default class
    /// </summary>
    public partial class Default : System.Web.UI.MasterPage
    {
        protected String username = "";
        //protected String mail = "";
        protected String servername = "";
        /// <summary>
        /// page init event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(object sender, EventArgs e)
        {
            //This section gets executed before any of the code in the child pages are executed.
            //So check if the user has an empty session, if so, check cookie and create session
            try
            {
                bool userHasSession = false;
                try
                {
                    if (Session["isLoggedIn"].Equals("Yes"))
                        userHasSession = true;
                }
#pragma warning disable CS0168 // The variable 'ee' is declared but never used
                catch (Exception ee)
#pragma warning restore CS0168 // The variable 'ee' is declared but never used
                {
                    userHasSession = false;
                }
                if (!userHasSession)
                {
                    SessionHandler sh = new SessionHandler();
                    sh.SetSessionFromFBA();
                }
                username = Session["name"].ToString();
                //SessionHandler.KeepAlive();
            }
            catch(Exception)
            {
                username = "";
            }
        }
        /// <summary>
        /// page load event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                username = Session["name"].ToString();
            }
            catch (Exception)
            {
                logout_Click(sender, e);
            }
            try
            {
                FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(String.Format("{0}\\bin\\ApprovalWorkflowApp.dll", HttpRuntime.AppDomainAppPath));
                List<string> build = myFileVersionInfo.FileVersion.Split('.').ToList();
                lblBuildNumber.Text = String.Format("Version {0}.{1} Build#{2}.{3}",build[0], build[1], build[2], build[3]);
            }
            catch (Exception)
            {
                lblBuildNumber.Text = "";
            }
            try
            {
                servername = String.Format("({0})",System.Net.Dns.GetHostName());
            }
            catch (Exception)
            {
                servername = "";
            }
        }
        /// <summary>
        /// logout click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void logout_Click(object sender, EventArgs e)
        {
            SessionHandler.LogOut();
        }
    }
}