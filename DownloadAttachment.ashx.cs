using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SRAFDBConnection.Utils;
using System.Data;
namespace ApprovalWorkflowApp
{
    /// <summary>
    /// Summary description for DownloadAttachment
    /// </summary>
    public class DownloadAttachment : IHttpHandler
    {
        /// <summary>
        /// process the request
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            string id = string.Empty;
            try
            {
                id = context.Request.QueryString["id"].ToString();
            }
            catch (Exception)
            {
                id = "";
            }
            if (!id.Length.Equals(0))
            {
                id = Utils.Crypto.Decrypt(id);
                SQLServerConnection obj = new SQLServerConnection();
                DataTable data = obj.GetCommentAttachment(id);
                if (data != null && data.Rows.Count > 0)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(data.Rows[0]["CommentAttachment"].ToString()) && !string.IsNullOrEmpty(data.Rows[0]["FileName"].ToString()))
                        {
                            byte[] fileContent = Convert.FromBase64String(data.Rows[0]["CommentAttachment"].ToString());
                            string[] stringParts = data.Rows[0]["FileName"].ToString().Split(new char[] { '.' });
                            string strType = stringParts[1];
                            HttpContext.Current.Response.Clear();
                            HttpContext.Current.Response.ClearContent();
                            HttpContext.Current.Response.ClearHeaders();
                            HttpContext.Current.Response.AddHeader("content-disposition", String.Format("attachment; filename={0}",data.Rows[0]["FileName"].ToString()));
                            //Set the content type as file extension type
                            HttpContext.Current.Response.ContentType = strType;
                            //Write the file content
                            HttpContext.Current.Response.BinaryWrite(fileContent);
                            HttpContext.Current.Response.End();
                        }
                    }
                    catch (Exception)
                    {
                        id = "";
                    }
                }
            }
        }
        /// <summary>
        /// checks if its reusable
        /// </summary>
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}