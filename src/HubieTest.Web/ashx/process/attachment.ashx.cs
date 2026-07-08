using HubieTest.Business;
using HubieTest.Dal;
using HubieTest.Web.ashx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Web;

namespace HubieTest.Web.process
{
    /// <summary>
    /// Ticket attachment upload. Unlike the other handlers, it receives
    /// multipart/form-data (binary file), so it reads from Request.Files.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Expected flow (method = "upload"):
    ///   1. validate that a file and a ticketId were sent;
    ///   2. save the file to disk (e.g. ~/uploads/{ticketId}/{guid}_{name});
    ///   3. register the metadata via ticketBusiness.registerAttachment(...);
    ///   4. return the created ATTACHMENT as JSON.
    /// Download/listing can be done by ticket.ashx (listAttachments) + a
    /// static/endpoint route that serves the saved file.
    /// ==================================================================
    /// </summary>
    public class attachment : AshxBase
    {
        private static readonly long MAX_BYTES = 10 * 1024 * 1024; // 10 MB
        private static readonly HashSet<string> ALLOWED_EXT = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx",
            ".txt", ".zip"
        };

        public override void ProcessRequest(HttpContext context)
        {
            base.ProcessRequestSafe(context); // validates JWT
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;

            if (HttpStatusReturn != 200)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = HttpStatusReturn;
                context.Response.Write(string.Empty);
                return;
            }

            if (strMETHOD == "download")
            {
                processDownload(context);
                return;
            }

            context.Response.ContentType = "application/json";
            strContextResponse = processUpload(context);
            context.Response.StatusCode = HttpStatusReturn;
            context.Response.Write(strContextResponse);
        }

        // ──────────────────────────── UPLOAD ────────────────────────────

        private string processUpload(HttpContext context)
        {
            if (strMETHOD != "upload")
            {
                HttpStatusReturn = 400;
                return JsonConvert.SerializeObject(new { error = "Unsupported method: " + strMETHOD });
            }

            try
            {
                // ── 1. read form fields ──────────────────────────────────
                string ticketIdStr = context.Request.Form["ticketId"];
                if (string.IsNullOrEmpty(ticketIdStr) || !long.TryParse(ticketIdStr, out long ticketId))
                {
                    HttpStatusReturn = 400;
                    return JsonConvert.SerializeObject(new { error = "Missing or invalid ticketId." });
                }

                if (context.Request.Files.Count == 0 || context.Request.Files[0].ContentLength == 0)
                {
                    HttpStatusReturn = 400;
                    return JsonConvert.SerializeObject(new { error = "No file provided." });
                }

                HttpPostedFile file = context.Request.Files[0];

                // ── 2. validate ──────────────────────────────────────────
                if (file.ContentLength > MAX_BYTES)
                {
                    HttpStatusReturn = 400;
                    return JsonConvert.SerializeObject(new { error = "File exceeds 10 MB limit." });
                }

                string ext = Path.GetExtension(file.FileName);
                if (!ALLOWED_EXT.Contains(ext))
                {
                    HttpStatusReturn = 400;
                    return JsonConvert.SerializeObject(new
                    {
                        error = $"Extension '{ext}' not allowed. Accepted: jpg, jpeg, png, gif, pdf, doc, docx, xls, xlsx, txt, zip."
                    });
                }

                // ── 3. save to disk ──────────────────────────────────────
                string safeName = Path.GetFileName(file.FileName); // prevent path traversal
                string guid = Guid.NewGuid().ToString("N");
                string fileName = $"{guid}_{safeName}";

                string baseDir = context.Server.MapPath($"~/App_Data/uploads/{ticketId}");
                string fullPath = Path.Combine(baseDir, fileName);
                string relPath = $"App_Data/uploads/{ticketId}/{fileName}";

                if (!Directory.Exists(baseDir))
                    Directory.CreateDirectory(baseDir);

                file.SaveAs(fullPath);

                // ── 4. register metadata ──────────────────────────────────
                var business = new ticketBusiness
                {
                    loggedUserId = UserId,
                    loggedUserName = UserName,
                    loggedUserProfile = UserProfile
                };

                var att = new ATTACHMENT
                {
                    TICKET_ID = ticketId,
                    ATTACHMENT_NAME = safeName,
                    ATTACHMENT_TYPE = file.ContentType,
                    ATTACHMENT_SIZE = file.ContentLength,
                    ATTACHMENT_PATH = relPath
                };

                var saved = business.registerAttachment(att);
                return JsonConvert.SerializeObject(saved);
            }
            catch (UnauthorizedAccessException ex)
            {
                HttpStatusReturn = 403;
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                HttpStatusReturn = 404;
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Upload error: " + ex.Message });
            }
        }

        // ──────────────────────────── DOWNLOAD ──────────────────────────

        private void processDownload(HttpContext context)
        {
            try
            {
                string ticketIdStr = context.Request.QueryString["ticketId"];
                string attachmentIdStr = context.Request.QueryString["attachmentId"];

                if (!long.TryParse(ticketIdStr, out long ticketId) ||
                    !long.TryParse(attachmentIdStr, out long attachmentId))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 400;
                    context.Response.Write(JsonConvert.SerializeObject(new { error = "Invalid parameters." }));
                    return;
                }

                // Verify access rights via business layer
                var business = new ticketBusiness
                {
                    loggedUserId = UserId,
                    loggedUserName = UserName,
                    loggedUserProfile = UserProfile
                };

                // listAttachments will throw if access is denied
                var attachments = business.listAttachments(ticketId);
                ATTACHMENT target = null;
                foreach (var a in attachments)
                {
                    if (a.ATTACHMENT_ID == attachmentId) { target = a; break; }
                }

                if (target == null)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 404;
                    context.Response.Write(JsonConvert.SerializeObject(new { error = "Attachment not found." }));
                    return;
                }

                string fullPath = context.Server.MapPath("~/" + target.ATTACHMENT_PATH);
                if (!File.Exists(fullPath))
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 404;
                    context.Response.Write(JsonConvert.SerializeObject(new { error = "File not found on disk." }));
                    return;
                }

                context.Response.ContentType = string.IsNullOrEmpty(target.ATTACHMENT_TYPE)
                    ? "application/octet-stream"
                    : target.ATTACHMENT_TYPE;

                context.Response.AddHeader("Content-Disposition",
                    $"attachment; filename=\"{target.ATTACHMENT_NAME}\"");

                context.Response.TransmitFile(fullPath);
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 403;
                context.Response.Write(JsonConvert.SerializeObject(new { error = ex.Message }));
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 500;
                context.Response.Write(JsonConvert.SerializeObject(new { error = "Download error: " + ex.Message }));
            }
        }
    }
}
