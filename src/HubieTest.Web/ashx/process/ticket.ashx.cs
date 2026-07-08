using HubieTest.Business;
using HubieTest.Web.ashx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web;

namespace HubieTest.Web.process
{
    /// <summary>
    /// TICKET handler. Mirrors process/ticket.ashx in Hubie: a single .ashx that
    /// dispatches several operations through the "method" field.
    ///
    /// ========================= CANDIDATE AREA =========================
    /// Implement each "case" of the switch following the categories.ashx model:
    ///   1. deserialize "data" (Newtonsoft) into the proper object/entity;
    ///   2. call the matching method on ticketBusiness;
    ///   3. serialize the result to JSON (JsonConvert.SerializeObject).
    ///
    /// IMPORTANT (security): the logged-in user id/profile/name ALREADY come from
    /// the token (UserId/UserProfile/UserName, filled by AshxBase). Use them —
    /// never trust a user id coming from the request body.
    ///
    /// "method" contract expected by the frontend (keep these names):
    ///   open | listMine | listQueue | get | assign | changeStatus |
    ///   addInteraction | listInteractions | listAttachments
    /// ==================================================================
    /// </summary>
    public class ticket : AshxBase
    {
        public override void ProcessRequest(HttpContext context)
        {
            base.ProcessRequestSafe(context); // validates JWT
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.ContentType = "application/json";

            if (HttpStatusReturn == 200)
            {
                strContextResponse = processRequest(strMETHOD, strData);
            }

            context.Response.StatusCode = HttpStatusReturn;
            context.Response.Write(strContextResponse);
        }

        private string processRequest(string method, string data)
        {
            // Inject logged-in user (from validated JWT) into the business layer
            var business = new ticketBusiness
            {
                loggedUserId = UserId,
                loggedUserName = UserName,
                loggedUserProfile = UserProfile
            };

            try
            {
                switch (method)
                {
                    // ── REQUESTER: open a new ticket ───────────────────────────────
                    case "open":
                        {
                            var t = JObject.Parse(data ?? "{}");
                            var newTicket = new HubieTest.Dal.TICKET
                            {
                                TICKET_TITLE = (string)t["TICKET_TITLE"],
                                TICKET_DESCRIPTION = (string)t["TICKET_DESCRIPTION"],
                                CATEGORY_ID = t["CATEGORY_ID"] != null ? (long)t["CATEGORY_ID"] : 0,
                                CATEGORY_NAME = (string)t["CATEGORY_NAME"]
                            };
                            var created = business.open(newTicket);
                            return JsonConvert.SerializeObject(created);
                        }

                    // ── REQUESTER: list their own tickets ──────────────────────────
                    case "listMine":
                        return JsonConvert.SerializeObject(business.listMyTickets());

                    // ── AGENT: service queue (optional status filter) ───────────────
                    case "listQueue":
                        {
                            string status = null;
                            if (!string.IsNullOrEmpty(data))
                            {
                                var d = JObject.Parse(data);
                                status = (string)d["status"];
                            }
                            return JsonConvert.SerializeObject(business.listQueue(status));
                        }

                    // ── BOTH: ticket header ────────────────────────────────────────
                    case "get":
                        {
                            long id = parseId(data, "ticketId");
                            return JsonConvert.SerializeObject(business.get(id));
                        }

                    // ── AGENT: take the ticket ─────────────────────────────────────
                    case "assign":
                        {
                            long id = parseId(data, "ticketId");
                            business.assign(id);
                            return JsonConvert.SerializeObject(new { success = true });
                        }

                    // ── AGENT (+ REQUESTER for CLOSED): change status ──────────────
                    case "changeStatus":
                        {
                            var d = JObject.Parse(data ?? "{}");
                            long id = (long)d["ticketId"];
                            string s = (string)d["status"];
                            business.changeStatus(id, s);
                            return JsonConvert.SerializeObject(new { success = true });
                        }

                    // ── BOTH: add a message to the thread ─────────────────────────
                    case "addInteraction":
                        {
                            var d = JObject.Parse(data ?? "{}");
                            long id = (long)d["ticketId"];
                            string msg = (string)d["message"];
                            var created = business.addInteraction(id, msg);
                            return JsonConvert.SerializeObject(created);
                        }

                    // ── BOTH: list interactions ───────────────────────────────────
                    case "listInteractions":
                        {
                            long id = parseId(data, "ticketId");
                            return JsonConvert.SerializeObject(business.listInteractions(id));
                        }

                    // ── BOTH: list attachments ────────────────────────────────────
                    case "listAttachments":
                        {
                            long id = parseId(data, "ticketId");
                            return JsonConvert.SerializeObject(business.listAttachments(id));
                        }

                    default:
                        HttpStatusReturn = 400;
                        return JsonConvert.SerializeObject(new { error = "Unsupported method: " + method });
                }
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
            catch (ArgumentException ex)
            {
                HttpStatusReturn = 400;
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                HttpStatusReturn = 422;
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                HttpStatusReturn = 500;
                return JsonConvert.SerializeObject(new { error = "Internal error: " + ex.Message });
            }
        }

        /// <summary>Parses a ticketId from the JSON data field.</summary>
        private long parseId(string data, string fieldName)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException($"Missing field: {fieldName}");

            var d = JObject.Parse(data);
            var v = d[fieldName];
            if (v == null)
                throw new ArgumentException($"Missing field: {fieldName}");

            return (long)v;
        }
    }
}
